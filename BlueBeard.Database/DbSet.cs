using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MySqlConnector;

namespace BlueBeard.Database;

public class DbSet<T>(Func<MySqlConnection> connectionFactory)
    where T : new()
{
    private readonly TableMetadata _metadata = TableMetadata.For<T>();

    public async Task<List<T>> QueryAsync()
    {
        var sql = $"SELECT * FROM `{_metadata.TableName}`;";
        return await QueryInternalAsync(sql, null);
    }

    /// <summary>
    /// Hydrate entities from arbitrary SELECT — joins, LIKE, anything the visitor can't translate.
    /// </summary>
    public async Task<List<T>> QuerySqlAsync(string sql, params (string name, object value)[] parameters)
    {
        return await QueryInternalAsync(sql, cmd =>
        {
            foreach (var (n, v) in parameters)
                cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        });
    }

    /// <summary>
    /// Non-query escape hatch (UPDATE/DELETE/DDL/etc) returning rows-affected.
    /// </summary>
    public async Task<int> ExecuteSqlAsync(string sql, params (string name, object value)[] parameters)
    {
        using var conn = connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        foreach (var (n, v) in parameters)
            cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<T>> Where(Expression<Func<T, bool>> predicate)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"SELECT * FROM `{_metadata.TableName}` WHERE {whereSql};";
        return await QueryInternalAsync(sql, cmd => AddParameters(cmd, parameters));
    }

    public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"SELECT * FROM `{_metadata.TableName}` WHERE {whereSql} LIMIT 1;";
        var results = await QueryInternalAsync(sql, cmd => AddParameters(cmd, parameters));
        return results.Count > 0 ? results[0] : default;
    }

    public async Task InsertAsync(T entity)
    {
        var insertCols = _metadata.Columns.Where(c => !c.IsAutoIncrement).ToList();
        var colNames = string.Join(", ", insertCols.Select(c => $"`{c.ColumnName}`"));
        var paramNames = string.Join(", ", insertCols.Select((_, i) => $"@p{i}"));
        var sql = $"INSERT INTO `{_metadata.TableName}` ({colNames}) VALUES ({paramNames}); SELECT LAST_INSERT_ID();";

        using var conn = connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        for (var i = 0; i < insertCols.Count; i++)
        {
            var col = insertCols[i];
            var value = col.PropertyInfo.GetValue(entity);
            cmd.Parameters.AddWithValue($"@p{i}", EntityReader.ToParameter(col, value));
        }

        var lastId = await cmd.ExecuteScalarAsync();
        if (_metadata.PrimaryKey is { IsAutoIncrement: true } pk && lastId != null && lastId != DBNull.Value)
        {
            var id = Convert.ChangeType(lastId, pk.ClrType);
            pk.PropertyInfo.SetValue(entity, id);
        }
    }

    public async Task UpdateAsync(T entity)
    {
        if (_metadata.PrimaryKey == null)
            throw new InvalidOperationException($"Cannot update {typeof(T).Name}: no primary key defined.");

        var updateCols = _metadata.Columns.Where(c => !c.IsPrimaryKey).ToList();
        var setClauses = updateCols.Select((c, i) => $"`{c.ColumnName}` = @p{i}").ToList();
        var pkParamIndex = updateCols.Count;
        var sql = $"UPDATE `{_metadata.TableName}` SET {string.Join(", ", setClauses)} " +
                  $"WHERE `{_metadata.PrimaryKey.ColumnName}` = @p{pkParamIndex};";

        using var conn = connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        for (var i = 0; i < updateCols.Count; i++)
        {
            var col = updateCols[i];
            cmd.Parameters.AddWithValue($"@p{i}",
                EntityReader.ToParameter(col, col.PropertyInfo.GetValue(entity)));
        }
        cmd.Parameters.AddWithValue($"@p{pkParamIndex}",
            EntityReader.ToParameter(_metadata.PrimaryKey, _metadata.PrimaryKey.PropertyInfo.GetValue(entity)));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        if (_metadata.PrimaryKey == null)
            throw new InvalidOperationException($"Cannot delete {typeof(T).Name}: no primary key defined.");

        var sql = $"DELETE FROM `{_metadata.TableName}` WHERE `{_metadata.PrimaryKey.ColumnName}` = @p0;";

        using var conn = connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@p0",
            EntityReader.ToParameter(_metadata.PrimaryKey, _metadata.PrimaryKey.PropertyInfo.GetValue(entity)));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"DELETE FROM `{_metadata.TableName}` WHERE {whereSql};";

        using var conn = connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<List<T>> QueryInternalAsync(string sql, Action<MySqlCommand> bindParameters)
    {
        using var conn = connectionFactory();
        await conn.OpenAsync();

        List<object> results;
        using (var cmd = new MySqlCommand(sql, conn))
        {
            bindParameters?.Invoke(cmd);
            using var reader = await cmd.ExecuteReaderAsync();
            results = await EntityReader.ReadAllAsync(reader, _metadata);
        }

        // Reader is closed; safe to issue the navigation queries on the same connection.
        if (_metadata.Navigations.Count > 0 && results.Count > 0)
            await PopulateNavigationsAsync(results, conn);

        return results.Cast<T>().ToList();
    }

    private async Task PopulateNavigationsAsync(List<object> entities, MySqlConnection conn)
    {
        foreach (var nav in _metadata.Navigations)
        {
            if (nav.Kind == NavigationKind.HasMany)
                await PopulateHasManyAsync(entities, nav, conn);
            else
                await PopulateBelongsToAsync(entities, nav, conn);
        }
    }

    private async Task PopulateHasManyAsync(List<object> parents, NavigationInfo nav, MySqlConnection conn)
    {
        if (_metadata.PrimaryKey == null)
            throw new InvalidOperationException(
                $"[HasMany] on {_metadata.ClrType.Name}.{nav.PropertyInfo.Name} requires the parent type to have a [PrimaryKey].");

        var relatedMeta = TableMetadata.For(nav.ElementType);
        var fkCol = relatedMeta.GetColumnByPropertyName(nav.ForeignKeyProperty)
            ?? throw new InvalidOperationException(
                $"[HasMany] on {_metadata.ClrType.Name}.{nav.PropertyInfo.Name} references " +
                $"property '{nav.ForeignKeyProperty}' on {nav.ElementType.Name}, which is not a mapped column.");

        // Initialize empty collections so consumers never see null even when there are no children.
        var listType = typeof(List<>).MakeGenericType(nav.ElementType);
        foreach (var parent in parents)
            nav.PropertyInfo.SetValue(parent, Activator.CreateInstance(listType));

        var pkValues = parents
            .Select(p => _metadata.PrimaryKey.PropertyInfo.GetValue(p))
            .Where(v => v != null)
            .Distinct()
            .ToList();
        if (pkValues.Count == 0) return;

        // Single batched query: WHERE fk IN (@k0, @k1, ...) — not N+1.
        var paramNames = pkValues.Select((_, i) => $"@k{i}").ToList();
        var sql = $"SELECT * FROM `{relatedMeta.TableName}` " +
                  $"WHERE `{fkCol.ColumnName}` IN ({string.Join(", ", paramNames)});";

        List<object> children;
        using (var cmd = new MySqlCommand(sql, conn))
        {
            for (var i = 0; i < pkValues.Count; i++)
                cmd.Parameters.AddWithValue(paramNames[i], EntityReader.ToParameter(fkCol, pkValues[i]));
            using var reader = await cmd.ExecuteReaderAsync();
            children = await EntityReader.ReadAllAsync(reader, relatedMeta);
        }

        var fkProp = fkCol.PropertyInfo;
        var childrenByFk = children
            .GroupBy(c => fkProp.GetValue(c))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var parent in parents)
        {
            var pkValue = _metadata.PrimaryKey.PropertyInfo.GetValue(parent);
            if (pkValue == null || !childrenByFk.TryGetValue(pkValue, out var matched)) continue;

            var list = (IList)nav.PropertyInfo.GetValue(parent);
            foreach (var child in matched)
                list.Add(child);
        }
    }

    private async Task PopulateBelongsToAsync(List<object> children, NavigationInfo nav, MySqlConnection conn)
    {
        var localKeyCol = _metadata.GetColumnByPropertyName(nav.LocalKeyProperty)
            ?? throw new InvalidOperationException(
                $"[BelongsTo] on {_metadata.ClrType.Name}.{nav.PropertyInfo.Name} references " +
                $"local property '{nav.LocalKeyProperty}', which is not a mapped column.");

        var parentMeta = TableMetadata.For(nav.ElementType);
        if (parentMeta.PrimaryKey == null)
            throw new InvalidOperationException(
                $"[BelongsTo] target {nav.ElementType.Name} has no [PrimaryKey].");

        var keyValues = children
            .Select(c => localKeyCol.PropertyInfo.GetValue(c))
            .Where(v => v != null)
            .Distinct()
            .ToList();
        if (keyValues.Count == 0) return;

        var paramNames = keyValues.Select((_, i) => $"@k{i}").ToList();
        var sql = $"SELECT * FROM `{parentMeta.TableName}` " +
                  $"WHERE `{parentMeta.PrimaryKey.ColumnName}` IN ({string.Join(", ", paramNames)});";

        List<object> parents;
        using (var cmd = new MySqlCommand(sql, conn))
        {
            for (var i = 0; i < keyValues.Count; i++)
                cmd.Parameters.AddWithValue(paramNames[i],
                    EntityReader.ToParameter(parentMeta.PrimaryKey, keyValues[i]));
            using var reader = await cmd.ExecuteReaderAsync();
            parents = await EntityReader.ReadAllAsync(reader, parentMeta);
        }

        var parentByKey = parents.ToDictionary(p => parentMeta.PrimaryKey.PropertyInfo.GetValue(p));

        foreach (var child in children)
        {
            var key = localKeyCol.PropertyInfo.GetValue(child);
            if (key == null) continue;
            if (parentByKey.TryGetValue(key, out var parent))
                nav.PropertyInfo.SetValue(child, parent);
        }
    }

    private static void AddParameters(MySqlCommand cmd, List<object> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
            cmd.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BlueBeard.Database.Attributes;
using MySqlConnector;

namespace BlueBeard.Database;

public class DbSet<T>(Func<MySqlConnection> connectionFactory)
    where T : new()
{
    private readonly TableMetadata _metadata = TableMetadata.For<T>();

    internal TableMetadata Metadata => _metadata;
    internal Func<MySqlConnection> ConnectionFactory => connectionFactory;

    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<List<T>> QueryAsync()
    {
        var sql = $"SELECT * FROM {SqlIdentifier.Quote(_metadata.TableName)};";
        return await QueryInternalAsync(sql, null);
    }

    /// <summary>Same as <see cref="QueryAsync()"/> but reads inside the given transaction.</summary>
    public async Task<List<T>> QueryAsync(BbTransaction transaction)
    {
        var sql = $"SELECT * FROM {SqlIdentifier.Quote(_metadata.TableName)};";
        return await QueryInternalAsync(sql, null, transaction);
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
        var sql = $"SELECT * FROM {SqlIdentifier.Quote(_metadata.TableName)} WHERE {whereSql};";
        return await QueryInternalAsync(sql, cmd => AddParameters(cmd, parameters));
    }

    /// <summary>Same as <see cref="Where(Expression{Func{T, bool}})"/> but inside the given transaction.</summary>
    public async Task<List<T>> Where(Expression<Func<T, bool>> predicate, BbTransaction transaction)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"SELECT * FROM {SqlIdentifier.Quote(_metadata.TableName)} WHERE {whereSql};";
        return await QueryInternalAsync(sql, cmd => AddParameters(cmd, parameters), transaction);
    }

    public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"SELECT * FROM {SqlIdentifier.Quote(_metadata.TableName)} WHERE {whereSql} LIMIT 1;";
        var results = await QueryInternalAsync(sql, cmd => AddParameters(cmd, parameters));
        return results.Count > 0 ? results[0] : default;
    }

    /// <summary>Same as <see cref="FirstOrDefaultAsync(Expression{Func{T, bool}})"/> but inside the given transaction.</summary>
    public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, BbTransaction transaction)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"SELECT * FROM {SqlIdentifier.Quote(_metadata.TableName)} WHERE {whereSql} LIMIT 1;";
        var results = await QueryInternalAsync(sql, cmd => AddParameters(cmd, parameters), transaction);
        return results.Count > 0 ? results[0] : default;
    }

    public Task<long> CountAsync() => ScalarLongAsync(
        $"SELECT COUNT(*) FROM {SqlIdentifier.Quote(_metadata.TableName)};", null);

    public Task<long> CountAsync(Expression<Func<T, bool>> predicate)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        return ScalarLongAsync(
            $"SELECT COUNT(*) FROM {SqlIdentifier.Quote(_metadata.TableName)} WHERE {whereSql};",
            cmd => AddParameters(cmd, parameters));
    }

    public async Task<bool> AnyAsync() =>
        await ScalarLongAsync(
            $"SELECT EXISTS(SELECT 1 FROM {SqlIdentifier.Quote(_metadata.TableName)});", null) != 0;

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        return await ScalarLongAsync(
            $"SELECT EXISTS(SELECT 1 FROM {SqlIdentifier.Quote(_metadata.TableName)} WHERE {whereSql});",
            cmd => AddParameters(cmd, parameters)) != 0;
    }

    /// <summary>
    /// Composable query: <c>Query().Where(...).OrderBy(x =&gt; x.Score).Take(10).ToListAsync()</c>.
    /// </summary>
    public DbQuery<T> Query() => new(this);

    // -----------------------------------------------------------------------
    // Writes
    // -----------------------------------------------------------------------

    public async Task InsertAsync(T entity)
    {
        using var conn = connectionFactory();
        await conn.OpenAsync();
        await InsertCoreAsync(entity, conn, null);
    }

    /// <summary>Insert inside the given transaction.</summary>
    public Task InsertAsync(T entity, BbTransaction transaction) =>
        InsertCoreAsync(entity, transaction.Connection, transaction.Transaction);

    /// <summary>
    /// Multi-row insert: one round-trip per chunk instead of one per entity. Lifecycle
    /// hooks fire per entity. Auto-increment IDs are assigned back sequentially (MySQL
    /// allocates consecutive IDs for a single multi-row INSERT).
    /// </summary>
    public async Task InsertRangeAsync(IEnumerable<T> entities)
    {
        using var conn = connectionFactory();
        await conn.OpenAsync();
        await InsertRangeCoreAsync(entities, conn, null);
    }

    /// <summary>Multi-row insert inside the given transaction.</summary>
    public Task InsertRangeAsync(IEnumerable<T> entities, BbTransaction transaction) =>
        InsertRangeCoreAsync(entities, transaction.Connection, transaction.Transaction);

    public async Task UpdateAsync(T entity)
    {
        using var conn = connectionFactory();
        await conn.OpenAsync();
        await UpdateCoreAsync(entity, conn, null);
    }

    /// <summary>Update inside the given transaction.</summary>
    public Task UpdateAsync(T entity, BbTransaction transaction) =>
        UpdateCoreAsync(entity, transaction.Connection, transaction.Transaction);

    /// <summary>
    /// Update many entities over a single connection. Lifecycle hooks fire per entity.
    /// </summary>
    public async Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        using var conn = connectionFactory();
        await conn.OpenAsync();
        foreach (var entity in entities)
            await UpdateCoreAsync(entity, conn, null);
    }

    /// <summary>Update many entities inside the given transaction.</summary>
    public async Task UpdateRangeAsync(IEnumerable<T> entities, BbTransaction transaction)
    {
        foreach (var entity in entities)
            await UpdateCoreAsync(entity, transaction.Connection, transaction.Transaction);
    }

    public async Task DeleteAsync(T entity)
    {
        using var conn = connectionFactory();
        await conn.OpenAsync();
        await DeleteCoreAsync(entity, conn, null);
    }

    /// <summary>Delete inside the given transaction.</summary>
    public Task DeleteAsync(T entity, BbTransaction transaction) =>
        DeleteCoreAsync(entity, transaction.Connection, transaction.Transaction);

    /// <summary>
    /// Bulk delete by predicate. Lifecycle hooks do NOT fire — no entity instances exist
    /// to call them on (same semantics as EF bulk operations). Use the entity overload
    /// when hooks matter.
    /// </summary>
    public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"DELETE FROM {SqlIdentifier.Quote(_metadata.TableName)} WHERE {whereSql};";

        using var conn = connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Bulk delete by predicate inside the given transaction. Hooks do not fire.</summary>
    public async Task DeleteAsync(Expression<Func<T, bool>> predicate, BbTransaction transaction)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"DELETE FROM {SqlIdentifier.Quote(_metadata.TableName)} WHERE {whereSql};";

        using var cmd = new MySqlCommand(sql, transaction.Connection, transaction.Transaction);
        AddParameters(cmd, parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    // -----------------------------------------------------------------------
    // Write cores (shared by direct + transaction + range paths)
    // -----------------------------------------------------------------------

    private async Task InsertCoreAsync(T entity, MySqlConnection conn, MySqlTransaction tx)
    {
        await HookRunner.RunAsync(_metadata, HookKind.BeforeInsert, entity);

        var insertCols = _metadata.Columns.Where(c => !c.IsAutoIncrement).ToList();
        var colNames = string.Join(", ", insertCols.Select(c => SqlIdentifier.Quote(c.ColumnName)));
        var paramNames = string.Join(", ", insertCols.Select((_, i) => $"@p{i}"));
        var sql = $"INSERT INTO {SqlIdentifier.Quote(_metadata.TableName)} ({colNames}) VALUES ({paramNames});";

        using var cmd = new MySqlCommand(sql, conn, tx);
        for (var i = 0; i < insertCols.Count; i++)
        {
            var col = insertCols[i];
            cmd.Parameters.AddWithValue($"@p{i}", EntityReader.ToParameter(col, col.PropertyInfo.GetValue(entity)));
        }

        await cmd.ExecuteNonQueryAsync();

        if (_metadata.PrimaryKey is { IsAutoIncrement: true } pk && cmd.LastInsertedId != 0)
            pk.PropertyInfo.SetValue(entity, Convert.ChangeType(cmd.LastInsertedId, pk.ClrType));

        await HookRunner.RunAsync(_metadata, HookKind.AfterInsert, entity);
    }

    private async Task InsertRangeCoreAsync(IEnumerable<T> entities, MySqlConnection conn, MySqlTransaction tx)
    {
        var list = entities as IList<T> ?? entities.ToList();
        if (list.Count == 0) return;

        foreach (var entity in list)
            await HookRunner.RunAsync(_metadata, HookKind.BeforeInsert, entity);

        var insertCols = _metadata.Columns.Where(c => !c.IsAutoIncrement).ToList();
        var colNames = string.Join(", ", insertCols.Select(c => SqlIdentifier.Quote(c.ColumnName)));

        // Stay well under max_allowed_packet / parameter limits.
        var chunkSize = Math.Max(1, 2000 / Math.Max(1, insertCols.Count));
        var pk = _metadata.PrimaryKey is { IsAutoIncrement: true } p ? p : null;

        for (var offset = 0; offset < list.Count; offset += chunkSize)
        {
            var count = Math.Min(chunkSize, list.Count - offset);
            var rows = new List<string>(count);
            for (var r = 0; r < count; r++)
                rows.Add($"({string.Join(", ", insertCols.Select((_, c) => $"@p{r}_{c}"))})");

            var sql = $"INSERT INTO {SqlIdentifier.Quote(_metadata.TableName)} ({colNames}) VALUES {string.Join(", ", rows)};";

            using var cmd = new MySqlCommand(sql, conn, tx);
            for (var r = 0; r < count; r++)
            {
                var entity = list[offset + r];
                for (var c = 0; c < insertCols.Count; c++)
                {
                    var col = insertCols[c];
                    cmd.Parameters.AddWithValue($"@p{r}_{c}", EntityReader.ToParameter(col, col.PropertyInfo.GetValue(entity)));
                }
            }

            await cmd.ExecuteNonQueryAsync();

            if (pk != null && cmd.LastInsertedId != 0)
            {
                // MySQL allocates consecutive IDs for a single multi-row INSERT.
                for (var r = 0; r < count; r++)
                    pk.PropertyInfo.SetValue(list[offset + r], Convert.ChangeType(cmd.LastInsertedId + r, pk.ClrType));
            }
        }

        foreach (var entity in list)
            await HookRunner.RunAsync(_metadata, HookKind.AfterInsert, entity);
    }

    private async Task UpdateCoreAsync(T entity, MySqlConnection conn, MySqlTransaction tx)
    {
        if (_metadata.PrimaryKey == null)
            throw new InvalidOperationException($"Cannot update {typeof(T).Name}: no primary key defined.");

        await HookRunner.RunAsync(_metadata, HookKind.BeforeUpdate, entity);

        var updateCols = _metadata.Columns.Where(c => !c.IsPrimaryKey).ToList();
        var setClauses = updateCols.Select((c, i) => $"{SqlIdentifier.Quote(c.ColumnName)} = @p{i}").ToList();
        var pkParamIndex = updateCols.Count;
        var sql = $"UPDATE {SqlIdentifier.Quote(_metadata.TableName)} SET {string.Join(", ", setClauses)} " +
                  $"WHERE {SqlIdentifier.Quote(_metadata.PrimaryKey.ColumnName)} = @p{pkParamIndex};";

        using var cmd = new MySqlCommand(sql, conn, tx);
        for (var i = 0; i < updateCols.Count; i++)
        {
            var col = updateCols[i];
            cmd.Parameters.AddWithValue($"@p{i}",
                EntityReader.ToParameter(col, col.PropertyInfo.GetValue(entity)));
        }
        cmd.Parameters.AddWithValue($"@p{pkParamIndex}",
            EntityReader.ToParameter(_metadata.PrimaryKey, _metadata.PrimaryKey.PropertyInfo.GetValue(entity)));

        await cmd.ExecuteNonQueryAsync();

        await HookRunner.RunAsync(_metadata, HookKind.AfterUpdate, entity);
    }

    private async Task DeleteCoreAsync(T entity, MySqlConnection conn, MySqlTransaction tx)
    {
        if (_metadata.PrimaryKey == null)
            throw new InvalidOperationException($"Cannot delete {typeof(T).Name}: no primary key defined.");

        await HookRunner.RunAsync(_metadata, HookKind.BeforeDelete, entity);

        var sql = $"DELETE FROM {SqlIdentifier.Quote(_metadata.TableName)} " +
                  $"WHERE {SqlIdentifier.Quote(_metadata.PrimaryKey.ColumnName)} = @p0;";

        using var cmd = new MySqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@p0",
            EntityReader.ToParameter(_metadata.PrimaryKey, _metadata.PrimaryKey.PropertyInfo.GetValue(entity)));

        await cmd.ExecuteNonQueryAsync();

        await HookRunner.RunAsync(_metadata, HookKind.AfterDelete, entity);
    }

    // -----------------------------------------------------------------------
    // Internals
    // -----------------------------------------------------------------------

    private async Task<long> ScalarLongAsync(string sql, Action<MySqlCommand> bindParameters)
    {
        using var conn = connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        bindParameters?.Invoke(cmd);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    internal async Task<List<T>> QueryInternalAsync(string sql, Action<MySqlCommand> bindParameters, BbTransaction transaction = null)
    {
        if (transaction != null)
            return await QueryOnConnectionAsync(sql, bindParameters, transaction.Connection, transaction.Transaction);

        using var conn = connectionFactory();
        await conn.OpenAsync();
        return await QueryOnConnectionAsync(sql, bindParameters, conn, null);
    }

    private async Task<List<T>> QueryOnConnectionAsync(string sql, Action<MySqlCommand> bindParameters, MySqlConnection conn, MySqlTransaction tx)
    {
        List<object> results;
        using (var cmd = new MySqlCommand(sql, conn, tx))
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
        var fkCol = relatedMeta.GetColumnByPropertyName(nav.ForeignKeyProperty)   // name path (unobfuscated / no-FK-attr)
            ?? relatedMeta.GetForeignKeyColumnTo(_metadata.ClrType)               // durable fallback via [ForeignKey] type token
            ?? throw new InvalidOperationException(
                $"[HasMany] on {_metadata.ClrType.Name}.{nav.PropertyInfo.Name} could not resolve the foreign key on " +
                $"{nav.ElementType.Name} (property name '{nav.ForeignKeyProperty ?? "<none>"}', no unambiguous [ForeignKey] to {_metadata.ClrType.Name}).");

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
        var sql = $"SELECT * FROM {SqlIdentifier.Quote(relatedMeta.TableName)} " +
                  $"WHERE {SqlIdentifier.Quote(fkCol.ColumnName)} IN ({string.Join(", ", paramNames)});";

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
        var localKeyCol = _metadata.GetColumnByPropertyName(nav.LocalKeyProperty)   // name path (unobfuscated / no-FK-attr)
            ?? _metadata.GetForeignKeyColumnTo(nav.ElementType)                     // durable fallback via [ForeignKey] type token
            ?? throw new InvalidOperationException(
                $"[BelongsTo] on {_metadata.ClrType.Name}.{nav.PropertyInfo.Name} could not resolve the local foreign key " +
                $"(property name '{nav.LocalKeyProperty ?? "<none>"}', no unambiguous [ForeignKey] to {nav.ElementType.Name}).");

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
        var sql = $"SELECT * FROM {SqlIdentifier.Quote(parentMeta.TableName)} " +
                  $"WHERE {SqlIdentifier.Quote(parentMeta.PrimaryKey.ColumnName)} IN ({string.Join(", ", paramNames)});";

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

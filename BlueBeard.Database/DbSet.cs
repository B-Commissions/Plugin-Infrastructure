using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;

namespace BlueBeard.Database;

public class DbSet<T> where T : new()
{
    private readonly Func<MySqlConnection> _connectionFactory;
    private readonly TableMetadata _metadata;

    public DbSet(Func<MySqlConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _metadata = TableMetadata.For<T>();
    }

    public async Task<List<T>> QueryAsync()
    {
        var sql = $"SELECT * FROM `{_metadata.TableName}`;";
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        return await ReadAllAsync(reader);
    }

    // Hydrate entities from arbitrary SELECT — joins, LIKE, anything the visitor can't do
    public async Task<List<T>> QuerySqlAsync(string sql, params (string name, object value)[] parameters)
    {
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        foreach (var (n, v) in parameters)
            cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        using var reader = await cmd.ExecuteReaderAsync();
        return await ReadAllAsync(reader);
    }

    // Non-query (UPDATE/DELETE/DDL/etc) returning rows-affected
    public async Task<int> ExecuteSqlAsync(string sql, params (string name, object value)[] parameters)
    {
        using var conn = _connectionFactory();
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
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        using var reader = await cmd.ExecuteReaderAsync();
        return await ReadAllAsync(reader);
    }

    public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"SELECT * FROM `{_metadata.TableName}` WHERE {whereSql} LIMIT 1;";
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        using var reader = await cmd.ExecuteReaderAsync();
        var results = await ReadAllAsync(reader);
        return results.Count > 0 ? results[0] : default;
    }

    public async Task InsertAsync(T entity)
    {
        var insertCols = _metadata.Columns.Where(c => !c.IsAutoIncrement).ToList();
        var colNames = string.Join(", ", insertCols.Select(c => $"`{c.ColumnName}`"));
        var paramNames = string.Join(", ", insertCols.Select((_, i) => $"@p{i}"));
        var sql = $"INSERT INTO `{_metadata.TableName}` ({colNames}) VALUES ({paramNames}); SELECT LAST_INSERT_ID();";
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        for (var i = 0; i < insertCols.Count; i++)
        {
            var col = insertCols[i];
            var value = col.PropertyInfo.GetValue(entity);
            cmd.Parameters.AddWithValue($"@p{i}", ToParameter(col, value));
        }

        var lastId = await cmd.ExecuteScalarAsync();
        if (_metadata.PrimaryKey is { IsAutoIncrement: true } pk && lastId != null)
        {
            var id = Convert.ChangeType(lastId, pk.ClrType);
            pk.PropertyInfo.SetValue(entity, id);
        }
    }

    private static object ToParameter(ColumnInfo col, object clrValue)
    {
        if (clrValue == null) return DBNull.Value;
        return col.Converter != null ? col.Converter.ToProvider(clrValue) : clrValue;
    }

    public async Task UpdateAsync(T entity)
    {
        if (_metadata.PrimaryKey == null)
            throw new InvalidOperationException($"Cannot update {typeof(T).Name}: no primary key defined.");
        var updateCols = _metadata.Columns.Where(c => !c.IsPrimaryKey).ToList();
        var setClauses = updateCols.Select((c, i) => $"`{c.ColumnName}` = @p{i}").ToList();
        var pkParamIndex = updateCols.Count;
        var sql = $"UPDATE `{_metadata.TableName}` SET {string.Join(", ", setClauses)} WHERE `{_metadata.PrimaryKey.ColumnName}` = @p{pkParamIndex};";
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        for (var i = 0; i < updateCols.Count; i++)
        {
            var col = updateCols[i];
            var value = col.PropertyInfo.GetValue(entity);
            cmd.Parameters.AddWithValue($"@p{i}", ToParameter(col, value));
        }

        cmd.Parameters.AddWithValue($"@p{pkParamIndex}", _metadata.PrimaryKey.PropertyInfo.GetValue(entity));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        if (_metadata.PrimaryKey == null)
            throw new InvalidOperationException($"Cannot delete {typeof(T).Name}: no primary key defined.");
        var sql = $"DELETE FROM `{_metadata.TableName}` WHERE `{_metadata.PrimaryKey.ColumnName}` = @p0;";
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@p0", _metadata.PrimaryKey.PropertyInfo.GetValue(entity));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        var (whereSql, parameters) = SqlWhereVisitor.Translate(predicate);
        var sql = $"DELETE FROM `{_metadata.TableName}` WHERE {whereSql};";
        using var conn = _connectionFactory();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(sql, conn);
        AddParameters(cmd, parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AddParameters(MySqlCommand cmd, List<object> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
            cmd.Parameters.AddWithValue($"@p{i}", parameters[i] ?? DBNull.Value);
    }

    private async Task<List<T>> ReadAllAsync(IDataReader reader)
    {
        var results = new List<T>();
        var ordinalMap = new Dictionary<string, int>();
        for (var i = 0; i < reader.FieldCount; i++)
            ordinalMap[reader.GetName(i)] = i;
        while (await ((MySqlDataReader)reader).ReadAsync())
        {
            var entity = new T();
            foreach (var col in _metadata.Columns)
            {
                if (!ordinalMap.TryGetValue(col.ColumnName, out var ordinal)) continue;
                if (reader.IsDBNull(ordinal)) continue;
                var value = ReadValue(reader, ordinal, col);
                col.PropertyInfo.SetValue(entity, value);
            }

            results.Add(entity);
        }

        return results;
    }

    private static object ReadValue(IDataReader reader, int ordinal, ColumnInfo col)
    {
        if (col.Converter != null)
            return col.Converter.FromProvider(reader.GetValue(ordinal));

        // Unwrap Nullable<T> up front so every check below matches both T and T?.
        // The caller (ReadAllAsync) already handles DBNull by skipping ReadValue entirely,
        // so we only reach here for non-NULL values. Boxing a T and assigning it to a T?
        // property via PropertyInfo.SetValue works via implicit boxing conversion.
        var type = Nullable.GetUnderlyingType(col.ClrType) ?? col.ClrType;
        try
        {
            if (type == typeof(ulong)) return ((MySqlDataReader)reader).GetUInt64(ordinal);
            if (type == typeof(bool)) return reader.GetBoolean(ordinal);
            if (type == typeof(int)) return reader.GetInt32(ordinal);
            if (type == typeof(long)) return reader.GetInt64(ordinal);
            if (type == typeof(float)) return reader.GetFloat(ordinal);
            if (type == typeof(double)) return reader.GetDouble(ordinal);
            if (type == typeof(string)) return reader.GetString(ordinal);
            if (type == typeof(DateTime)) return reader.GetDateTime(ordinal);
            return type.IsEnum ? Enum.ToObject(type, reader.GetInt32(ordinal)) : Convert.ChangeType(reader.GetValue(ordinal), type);
        }
        catch (InvalidCastException)
        {
            // tolerant fallback — typed getter disagreed with column shape
            var raw = reader.GetValue(ordinal);
            if (type == typeof(string)) return raw is byte[] b ? Encoding.UTF8.GetString(b) : raw.ToString();
            return Convert.ChangeType(raw, type);
        }
    }
}
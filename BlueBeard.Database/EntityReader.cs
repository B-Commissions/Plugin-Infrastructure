using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;

namespace BlueBeard.Database;

/// <summary>
/// Centralizes entity hydration so navigation loading and the main DbSet read path share
/// the same conversion logic. Operates on metadata + reader; doesn't know about T.
/// </summary>
internal static class EntityReader
{
    public static async Task<List<object>> ReadAllAsync(IDataReader reader, TableMetadata metadata)
    {
        var results = new List<object>();
        var ordinalMap = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < reader.FieldCount; i++)
            ordinalMap[reader.GetName(i)] = i;

        while (await ((MySqlDataReader)reader).ReadAsync())
        {
            var entity = Activator.CreateInstance(metadata.ClrType);
            foreach (var col in metadata.Columns)
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

    public static object ReadValue(IDataReader reader, int ordinal, ColumnInfo col)
    {
        // Converter takes priority and gets the raw provider value to decide what to do with it.
        if (col.Converter != null)
            return col.Converter.FromProvider(reader.GetValue(ordinal));

        var type = Nullable.GetUnderlyingType(col.ClrType) ?? col.ClrType;
        try
        {
            if (type == typeof(ulong))    return ((MySqlDataReader)reader).GetUInt64(ordinal);
            if (type == typeof(bool))     return reader.GetBoolean(ordinal);
            if (type == typeof(int))      return reader.GetInt32(ordinal);
            if (type == typeof(long))     return reader.GetInt64(ordinal);
            if (type == typeof(float))    return reader.GetFloat(ordinal);
            if (type == typeof(double))   return reader.GetDouble(ordinal);
            if (type == typeof(string))   return reader.GetString(ordinal);
            if (type == typeof(DateTime)) return reader.GetDateTime(ordinal);
            return type.IsEnum
                ? Enum.ToObject(type, reader.GetInt32(ordinal))
                : Convert.ChangeType(reader.GetValue(ordinal), type);
        }
        catch (InvalidCastException)
        {
            // Tolerant fallback when the typed getter disagrees with the column shape.
            // Most common case: VARCHAR property reading from a binary column, or vice versa.
            var raw = reader.GetValue(ordinal);
            if (type == typeof(string))
                return raw is byte[] b ? Encoding.UTF8.GetString(b) : raw.ToString();
            try { return Convert.ChangeType(raw, type); }
            catch (Exception ex)
            {
                throw new InvalidCastException(
                    $"Column '{col.ColumnName}' returned {raw?.GetType().Name ?? "null"} " +
                    $"but property '{col.PropertyName}' is {type.Name}. " +
                    $"Consider registering an IValueConverter or using [ColumnConverter].", ex);
            }
        }
    }

    /// <summary>
    /// Convert a CLR value to a parameter value, applying the column's converter if any.
    /// Used by every site that binds CLR values to MySqlCommand parameters.
    /// </summary>
    public static object ToParameter(ColumnInfo col, object clrValue)
    {
        if (clrValue == null) return DBNull.Value;
        return col.Converter != null ? col.Converter.ToProvider(clrValue) : clrValue;
    }
}

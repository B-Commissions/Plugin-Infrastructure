using System;
using System.Collections.Generic;
using System.Text;

namespace BlueBeard.Database;

public static class SchemaSync
{
    private static readonly Dictionary<Type, string> TypeMap = new()
    {
        { typeof(int), "INT" },
        { typeof(long), "BIGINT" },
        { typeof(ulong), "BIGINT UNSIGNED" },
        { typeof(string), "VARCHAR(255)" },
        { typeof(bool), "TINYINT(1)" },
        { typeof(float), "FLOAT" },
        { typeof(double), "DOUBLE" },
        { typeof(DateTime), "DATETIME" }
    };

    public static string GenerateCreateTable(TableMetadata metadata)
    {
        var sb = new StringBuilder();
        sb.Append($"CREATE TABLE IF NOT EXISTS `{metadata.TableName}` (");
        var first = true;
        foreach (var col in metadata.Columns)
        {
            if (!first) sb.Append(", ");
            first = false;
            var sqlType = GetSqlType(col);
            sb.Append($"`{col.ColumnName}` {sqlType}");
            if (col.IsPrimaryKey) sb.Append(" PRIMARY KEY");
            if (col.IsAutoIncrement) sb.Append(" AUTO_INCREMENT");
        }
        sb.Append(");");
        return sb.ToString();
    }

    private static string GetSqlType(ColumnInfo col)
    {
        if (!string.IsNullOrEmpty(col.OverrideSqlType)) return col.OverrideSqlType;
        var type = Nullable.GetUnderlyingType(col.ClrType) ?? col.ClrType;
        if (TypeMap.TryGetValue(type, out var sqlType))
            return Nullable.GetUnderlyingType(col.ClrType) != null ? sqlType + " NULL" : sqlType;
        if (type.IsEnum) return "INT";
        throw new NotSupportedException($"CLR type '{type.FullName}' has no SQL mapping.");
    }
}

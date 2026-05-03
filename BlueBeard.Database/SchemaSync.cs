using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlueBeard.Database.Attributes;

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
            sb.Append($"`{col.ColumnName}` {GetSqlType(col)}");
            if (col.IsPrimaryKey) sb.Append(" PRIMARY KEY");
            if (col.IsAutoIncrement) sb.Append(" AUTO_INCREMENT");
        }

        // Inline foreign key constraints. The referenced table must already exist —
        // register parent entities before children in DatabaseManager.
        foreach (var col in metadata.Columns.Where(c => c.ForeignKey != null))
        {
            var fk = col.ForeignKey;
            var refMeta = TableMetadata.For(fk.ReferencedType);
            var refCol = refMeta.GetColumnByPropertyName(fk.ReferencedProperty)
                ?? throw new InvalidOperationException(
                    $"Foreign key on {metadata.TableName}.{col.ColumnName} references " +
                    $"{fk.ReferencedType.Name}.{fk.ReferencedProperty}, which is not a mapped column.");

            sb.Append(", ");
            sb.Append($"CONSTRAINT `fk_{metadata.TableName}_{col.ColumnName}` ");
            sb.Append($"FOREIGN KEY (`{col.ColumnName}`) ");
            sb.Append($"REFERENCES `{refMeta.TableName}`(`{refCol.ColumnName}`) ");
            sb.Append($"ON DELETE {ActionToSql(fk.OnDelete)} ON UPDATE {ActionToSql(fk.OnUpdate)}");
        }

        sb.Append(");");
        return sb.ToString();
    }

    public static string GetSqlType(ColumnInfo col)
    {
        if (!string.IsNullOrEmpty(col.OverrideSqlType))
            return col.OverrideSqlType;

        if (col.Converter != null)
            return Nullable.GetUnderlyingType(col.ClrType) != null
                ? col.Converter.DefaultSqlType + " NULL"
                : col.Converter.DefaultSqlType;

        var type = Nullable.GetUnderlyingType(col.ClrType) ?? col.ClrType;
        if (TypeMap.TryGetValue(type, out var sqlType))
            return Nullable.GetUnderlyingType(col.ClrType) != null ? sqlType + " NULL" : sqlType;

        if (type.IsEnum)
            return "INT";

        throw new NotSupportedException($"CLR type '{type.FullName}' has no SQL mapping.");
    }

    private static string ActionToSql(ReferentialAction a) => a switch
    {
        ReferentialAction.Restrict => "RESTRICT",
        ReferentialAction.Cascade  => "CASCADE",
        ReferentialAction.SetNull  => "SET NULL",
        ReferentialAction.NoAction => "NO ACTION",
        _ => "RESTRICT"
    };
}

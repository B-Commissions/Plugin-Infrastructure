using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BlueBeard.Database.Attributes;

namespace BlueBeard.Database;

public static class SchemaSync
{
    private static readonly Dictionary<Type, string> TypeMap = new()
    {
        { typeof(byte),     "TINYINT UNSIGNED" },
        { typeof(sbyte),    "TINYINT" },
        { typeof(short),    "SMALLINT" },
        { typeof(ushort),   "SMALLINT UNSIGNED" },
        { typeof(int),      "INT" },
        { typeof(uint),     "INT UNSIGNED" },
        { typeof(long),     "BIGINT" },
        { typeof(ulong),    "BIGINT UNSIGNED" },
        { typeof(string),   "VARCHAR(255)" },
        { typeof(bool),     "TINYINT(1)" },
        { typeof(float),    "FLOAT" },
        { typeof(double),   "DOUBLE" },
        { typeof(DateTime), "DATETIME" }
    };

    public static string GenerateCreateTable(TableMetadata metadata)
    {
        var sb = new StringBuilder();
        sb.Append($"CREATE TABLE IF NOT EXISTS {SqlIdentifier.Quote(metadata.TableName)} (");

        var first = true;
        foreach (var col in metadata.Columns)
        {
            if (!first) sb.Append(", ");
            first = false;
            sb.Append($"{SqlIdentifier.Quote(col.ColumnName)} {GetColumnDefinition(col)}");
            if (col.IsPrimaryKey) sb.Append(" PRIMARY KEY");
            if (col.IsAutoIncrement) sb.Append(" AUTO_INCREMENT");
        }

        // Inline foreign key constraints. The referenced table must already exist —
        // register parent entities before children in DatabaseManager.
        foreach (var col in metadata.Columns.Where(c => c.ForeignKey != null))
        {
            var fk = col.ForeignKey;
            var refMeta = TableMetadata.For(fk.ReferencedType);
            // Name path first (unobfuscated); fall back to the referenced table's primary key,
            // since [ForeignKey] references are documented as "typically the primary key" and the
            // property-name string does not survive obfuscation while the PK column is durable.
            var refCol = refMeta.GetColumnByPropertyName(fk.ReferencedProperty)
                ?? refMeta.PrimaryKey
                ?? throw new InvalidOperationException(
                    $"Foreign key on {metadata.TableName}.{col.ColumnName} references " +
                    $"{fk.ReferencedType.Name}.{fk.ReferencedProperty}, which is not a mapped column.");

            sb.Append(", ");
            sb.Append($"CONSTRAINT {SqlIdentifier.Quote($"fk_{metadata.TableName}_{col.ColumnName}")} ");
            sb.Append($"FOREIGN KEY ({SqlIdentifier.Quote(col.ColumnName)}) ");
            sb.Append($"REFERENCES {SqlIdentifier.Quote(refMeta.TableName)}({SqlIdentifier.Quote(refCol.ColumnName)}) ");
            sb.Append($"ON DELETE {ActionToSql(fk.OnDelete)} ON UPDATE {ActionToSql(fk.OnUpdate)}");
        }

        // Secondary indexes from [Unique]/[Index].
        foreach (var index in metadata.Indexes)
        {
            sb.Append(", ");
            sb.Append(index.IsUnique ? "UNIQUE KEY " : "KEY ");
            sb.Append(SqlIdentifier.Quote(index.Name));
            sb.Append($" ({string.Join(", ", index.Columns.Select(c => SqlIdentifier.Quote(c.ColumnName)))})");
        }

        sb.Append(");");
        return sb.ToString();
    }

    /// <summary>
    /// The bare SQL type of a column — no nullability or default clause. Use
    /// <see cref="GetColumnDefinition"/> for the full DDL fragment.
    /// </summary>
    public static string GetSqlType(ColumnInfo col)
    {
        if (!string.IsNullOrEmpty(col.OverrideSqlType))
            return col.OverrideSqlType;

        if (col.Converter != null)
            return col.Converter.DefaultSqlType;

        var type = Nullable.GetUnderlyingType(col.ClrType) ?? col.ClrType;

        if (type == typeof(string) && col.MaxLength != null)
        {
            return col.MaxLength.Text || col.MaxLength.Length > MaxLengthAttribute.VarcharLimit
                ? "TEXT"
                : $"VARCHAR({col.MaxLength.Length})";
        }

        if (TypeMap.TryGetValue(type, out var sqlType))
            return sqlType;

        if (type.IsEnum)
            return "INT";

        throw new NotSupportedException($"CLR type '{type.FullName}' has no SQL mapping.");
    }

    /// <summary>
    /// Full column DDL fragment: type, explicit nullability, and default. Shared by
    /// CREATE TABLE and the migrator's ADD/MODIFY COLUMN statements.
    /// </summary>
    public static string GetColumnDefinition(ColumnInfo col)
    {
        var sb = new StringBuilder(GetSqlType(col));

        // Nullability clause only for explicit declarations. PK/AutoIncrement are NOT NULL
        // by MySQL's own rules; unannotated columns keep the historical implicit-nullable DDL
        // so schemas produced by older library versions stay byte-identical.
        if (col.HasExplicitNullability && !col.IsPrimaryKey)
            sb.Append(col.IsNullable ? " NULL" : " NOT NULL");

        if (col.Default != null)
            sb.Append($" DEFAULT {RenderDefault(col)}");

        return sb.ToString();
    }

    /// <summary>
    /// Render a [DefaultValue] as a SQL literal or expression.
    /// </summary>
    public static string RenderDefault(ColumnInfo col)
    {
        var def = col.Default;
        if (def.ServerDefault == ServerDefault.CurrentTimestamp)
            return "CURRENT_TIMESTAMP";

        var value = def.Value;
        if (value == null) return "NULL";

        // Route through the column's converter so e.g. a Guid default is stored in the
        // same representation the converter writes.
        if (col.Converter != null && value.GetType() == (Nullable.GetUnderlyingType(col.ClrType) ?? col.ClrType))
            value = col.Converter.ToProvider(value);

        return value switch
        {
            bool b => b ? "1" : "0",
            string s => $"'{s.Replace("\\", "\\\\").Replace("'", "''")}'",
            Enum e => Convert.ToInt32(e).ToString(CultureInfo.InvariantCulture),
            byte[] bytes => "0x" + BitConverter.ToString(bytes).Replace("-", ""),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => throw new NotSupportedException(
                $"[DefaultValue] on {col.PropertyName}: '{value.GetType().Name}' cannot be rendered as a SQL literal.")
        };
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

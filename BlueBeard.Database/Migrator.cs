using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySqlConnector;
using Rocket.Core.Logging;

namespace BlueBeard.Database;

internal static class Migrator
{
    internal sealed class ExistingColumn
    {
        public string ColumnType;
        public bool IsNullable;
        public string ColumnDefault;
        public string Extra;
    }

    public static async Task ApplyAsync(MySqlConnection conn, TableMetadata metadata, MigrationMode mode)
    {
        switch (mode)
        {
            case MigrationMode.Reset:
                await ExecuteAsync(conn, $"DROP TABLE IF EXISTS {SqlIdentifier.Quote(metadata.TableName)};");
                await ExecuteAsync(conn, SchemaSync.GenerateCreateTable(metadata));
                Logger.Log($"[Database] Reset table: {metadata.TableName}");
                return;

            case MigrationMode.None:
                await ExecuteAsync(conn, SchemaSync.GenerateCreateTable(metadata));
                Logger.Log($"[Database] Ensured table: {metadata.TableName}");
                return;

            case MigrationMode.Update:
                await ExecuteAsync(conn, SchemaSync.GenerateCreateTable(metadata));
                await UpdateAsync(conn, metadata);
                return;
        }
    }

    private static async Task UpdateAsync(MySqlConnection conn, TableMetadata metadata)
    {
        var existing = await GetExistingColumnsAsync(conn, metadata.TableName);
        var changes = 0;

        foreach (var col in metadata.Columns)
        {
            var definition = SchemaSync.GetColumnDefinition(col);
            // MODIFY/ADD without the AUTO_INCREMENT clause would silently drop it.
            if (col.IsAutoIncrement) definition += " AUTO_INCREMENT";

            if (!existing.TryGetValue(col.ColumnName.ToLowerInvariant(), out var current))
            {
                var sql = $"ALTER TABLE {SqlIdentifier.Quote(metadata.TableName)} " +
                          $"ADD COLUMN {SqlIdentifier.Quote(col.ColumnName)} {definition};";
                await ExecuteAsync(conn, sql);
                Logger.Log($"[Database] {metadata.TableName}: + {col.ColumnName} {definition}");
                changes++;
            }
            else if (NeedsModify(col, current, out var reason))
            {
                var sql = $"ALTER TABLE {SqlIdentifier.Quote(metadata.TableName)} " +
                          $"MODIFY COLUMN {SqlIdentifier.Quote(col.ColumnName)} {definition};";
                try
                {
                    await ExecuteAsync(conn, sql);
                    Logger.Log($"[Database] {metadata.TableName}: ~ {col.ColumnName} ({reason}) -> {definition}");
                    changes++;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex,
                        $"[Database] Could not migrate column {metadata.TableName}.{col.ColumnName} " +
                        $"({reason}) to '{definition}'. Existing data may be incompatible.");
                }
            }
        }

        changes += await EnsureIndexesAsync(conn, metadata);

        if (changes == 0)
            Logger.Log($"[Database] Up to date: {metadata.TableName}");

        // Columns that exist in the database but not in metadata are intentionally left alone.
        // Drops are destructive and require explicit user action (Reset mode or manual SQL).
    }

    /// <summary>
    /// Type, nullability, and default are diffed independently. Nullability is only enforced
    /// when the entity declares it explicitly, and defaults only when [DefaultValue] is present —
    /// so unannotated legacy entities never churn schemas created by older library versions.
    /// </summary>
    internal static bool NeedsModify(ColumnInfo col, ExistingColumn current, out string reason)
    {
        if (!TypesMatch(current.ColumnType, SchemaSync.GetSqlType(col)))
        {
            reason = $"type {current.ColumnType} -> {SchemaSync.GetSqlType(col)}";
            return true;
        }

        if (col.HasExplicitNullability && !col.IsPrimaryKey && current.IsNullable != col.IsNullable)
        {
            reason = $"nullability {(current.IsNullable ? "NULL" : "NOT NULL")} -> {(col.IsNullable ? "NULL" : "NOT NULL")}";
            return true;
        }

        if (col.Default != null &&
            !DefaultsMatch(current.ColumnDefault, SchemaSync.RenderDefault(col)))
        {
            reason = $"default {current.ColumnDefault ?? "(none)"} -> {SchemaSync.RenderDefault(col)}";
            return true;
        }

        reason = null;
        return false;
    }

    private static async Task<int> EnsureIndexesAsync(MySqlConnection conn, TableMetadata metadata)
    {
        if (metadata.Indexes.Count == 0) return 0;

        var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        const string sql = @"
            SELECT DISTINCT INDEX_NAME
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t;";
        using (var cmd = new MySqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@t", metadata.TableName);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                existingNames.Add(reader.GetString(0));
        }

        var created = 0;
        foreach (var index in metadata.Indexes.Where(i => !existingNames.Contains(i.Name)))
        {
            var cols = string.Join(", ", index.Columns.Select(c => SqlIdentifier.Quote(c.ColumnName)));
            var ddl = $"CREATE {(index.IsUnique ? "UNIQUE " : "")}INDEX {SqlIdentifier.Quote(index.Name)} " +
                      $"ON {SqlIdentifier.Quote(metadata.TableName)} ({cols});";
            try
            {
                await ExecuteAsync(conn, ddl);
                Logger.Log($"[Database] {metadata.TableName}: + index {index.Name} ({cols})");
                created++;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex,
                    $"[Database] Could not create index {index.Name} on {metadata.TableName}. " +
                    "For unique indexes this usually means existing rows violate uniqueness.");
            }
        }
        return created;
    }

    private static async Task<Dictionary<string, ExistingColumn>> GetExistingColumnsAsync(MySqlConnection conn, string tableName)
    {
        var result = new Dictionary<string, ExistingColumn>(StringComparer.OrdinalIgnoreCase);
        const string sql = @"
            SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, EXTRA
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t;";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@t", tableName);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result[reader.GetString(0).ToLowerInvariant()] = new ExistingColumn
            {
                ColumnType = reader.GetString(1),
                IsNullable = string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase),
                ColumnDefault = reader.IsDBNull(3) ? null : reader.GetString(3),
                Extra = reader.IsDBNull(4) ? null : reader.GetString(4)
            };
        }
        return result;
    }

    private static async Task ExecuteAsync(MySqlConnection conn, string sql)
    {
        using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    internal static bool TypesMatch(string current, string target) =>
        Normalize(current) == Normalize(target);

    /// <summary>
    /// Normalize a SQL type string for cross-comparison between INFORMATION_SCHEMA.COLUMN_TYPE
    /// (which lowercases and may include display widths on integer types) and our generated DDL.
    /// </summary>
    internal static string Normalize(string sqlType)
    {
        if (string.IsNullOrEmpty(sqlType)) return string.Empty;
        var s = sqlType.ToLowerInvariant().Trim();
        s = Regex.Replace(s, @"\s+", " ");

        // COLUMN_TYPE never carries nullability, so a stray clause on the target side must not
        // defeat the comparison (this exact mismatch used to emit a phantom MODIFY on every
        // startup for nullable columns).
        s = Regex.Replace(s, @" (not )?null$", "");

        // MySQL stores cosmetic display widths on integer types (e.g. `int(11)`, `bigint(20)`).
        // Strip them — they have no semantic meaning in modern MySQL. tinyint(1) is left intact
        // because it's the canonical boolean storage and meaningfully distinct from tinyint(4).
        s = Regex.Replace(s, @"\b(smallint|mediumint|int|bigint)\(\d+\)", "$1");
        return s;
    }

    /// <summary>
    /// Conservative default comparison across MySQL/MariaDB representation quirks
    /// (quoting, expression parens, CURRENT_TIMESTAMP() spelling). When in doubt, treat
    /// as equal — a skipped MODIFY is recoverable, a spurious one churns production DDL.
    /// </summary>
    internal static bool DefaultsMatch(string currentDefault, string renderedTarget)
    {
        var current = NormalizeDefault(currentDefault);
        var target = NormalizeDefault(renderedTarget);
        return current == target;
    }

    internal static string NormalizeDefault(string value)
    {
        if (value == null) return null;
        var s = value.Trim().ToLowerInvariant();
        // MariaDB wraps expression defaults in parentheses.
        while (s.Length >= 2 && s.StartsWith("(") && s.EndsWith(")"))
            s = s.Substring(1, s.Length - 2).Trim();
        // Strip literal quoting: our rendered form is 'text', INFORMATION_SCHEMA reports bare text.
        if (s.Length >= 2 && s.StartsWith("'") && s.EndsWith("'"))
            s = s.Substring(1, s.Length - 2).Replace("''", "'").Replace("\\\\", "\\");
        if (s == "current_timestamp()") s = "current_timestamp";
        if (s == "null") s = null;
        return s;
    }
}

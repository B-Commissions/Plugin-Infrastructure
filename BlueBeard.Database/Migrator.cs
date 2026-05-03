using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySqlConnector;
using Rocket.Core.Logging;

namespace BlueBeard.Database;

internal static class Migrator
{
    public static async Task ApplyAsync(MySqlConnection conn, TableMetadata metadata, MigrationMode mode)
    {
        switch (mode)
        {
            case MigrationMode.Reset:
                await ExecuteAsync(conn, $"DROP TABLE IF EXISTS `{metadata.TableName}`;");
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
            var target = SchemaSync.GetSqlType(col);

            if (!existing.TryGetValue(col.ColumnName.ToLowerInvariant(), out var current))
            {
                var sql = $"ALTER TABLE `{metadata.TableName}` ADD COLUMN `{col.ColumnName}` {target};";
                await ExecuteAsync(conn, sql);
                Logger.Log($"[Database] {metadata.TableName}: + {col.ColumnName} {target}");
                changes++;
            }
            else if (!TypesMatch(current, target))
            {
                var sql = $"ALTER TABLE `{metadata.TableName}` MODIFY COLUMN `{col.ColumnName}` {target};";
                try
                {
                    await ExecuteAsync(conn, sql);
                    Logger.Log($"[Database] {metadata.TableName}: ~ {col.ColumnName} {current} -> {target}");
                    changes++;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex,
                        $"[Database] Could not migrate column {metadata.TableName}.{col.ColumnName} " +
                        $"from '{current}' to '{target}'. Existing data may be incompatible.");
                }
            }
        }

        if (changes == 0)
            Logger.Log($"[Database] Up to date: {metadata.TableName}");

        // Columns that exist in the database but not in metadata are intentionally left alone.
        // Drops are destructive and require explicit user action (Reset mode or manual SQL).
    }

    private static async Task<Dictionary<string, string>> GetExistingColumnsAsync(MySqlConnection conn, string tableName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        const string sql = @"
            SELECT COLUMN_NAME, COLUMN_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t;";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@t", tableName);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            var type = reader.GetString(1);
            result[name.ToLowerInvariant()] = type;
        }
        return result;
    }

    private static async Task ExecuteAsync(MySqlConnection conn, string sql)
    {
        using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static bool TypesMatch(string current, string target) =>
        Normalize(current) == Normalize(target);

    /// <summary>
    /// Normalize a SQL type string for cross-comparison between INFORMATION_SCHEMA.COLUMN_TYPE
    /// (which lowercases and may include display widths on integer types) and our generated DDL.
    /// </summary>
    private static string Normalize(string sqlType)
    {
        if (string.IsNullOrEmpty(sqlType)) return string.Empty;
        var s = sqlType.ToLowerInvariant().Trim();
        s = Regex.Replace(s, @"\s+", " ");

        // MySQL stores cosmetic display widths on integer types (e.g. `int(11)`, `bigint(20)`).
        // Strip them — they have no semantic meaning in modern MySQL. tinyint(1) is left intact
        // because it's the canonical boolean storage and meaningfully distinct from tinyint(4).
        s = Regex.Replace(s, @"\b(smallint|mediumint|int|bigint)\(\d+\)", "$1");
        return s;
    }
}

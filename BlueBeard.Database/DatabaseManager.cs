using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BlueBeard.Core;
using BlueBeard.Core.Configs;
using BlueBeard.Core.Helpers;
using MySqlConnector;
using Rocket.Core.Logging;

namespace BlueBeard.Database;

public class DatabaseManager : IManager
{
    private string _connectionString;
    private readonly ConcurrentDictionary<Type, object> _dbSets = new();
    private readonly List<Type> _entityTypes = [];
    private DatabaseConfig _config;

    public void Initialize(ConfigManager configManager) =>
        _config = configManager.GetConfig<DatabaseConfig>();

    public void Initialize(DatabaseConfig config) =>
        _config = config;

    public void RegisterEntity<T>() where T : new()
    {
        _entityTypes.Add(typeof(T));
    }

    public void Load()
    {
        _connectionString = new MySqlConnectionStringBuilder
        {
            Server = _config.Host,
            Port = _config.Port,
            Database = _config.Database,
            UserID = _config.Username,
            Password = _config.Password
        }.ConnectionString;

        SyncSchema();
    }

    public void SyncSchema()
    {
        ThreadHelper.RunAsynchronously(() =>
        {
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                foreach (var type in _entityTypes)
                {
                    var metadata = TableMetadata.For(type);
                    var sql = SchemaSync.GenerateCreateTable(metadata);
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    Logger.Log($"[Database] Ensured table: {metadata.TableName}");
                }

                Logger.Log("[Database] Schema sync complete.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "[Database] Failed to sync schema.");
            }
        });
    }

    public void Unload() =>
        _dbSets.Clear();

    public DbSet<T> Table<T>() where T : new() =>
        (DbSet<T>)_dbSets.GetOrAdd(typeof(T), _ => new DbSet<T>(CreateConnection));

    private MySqlConnection CreateConnection() =>
        new(_connectionString);
}
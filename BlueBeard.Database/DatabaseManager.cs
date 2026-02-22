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
    private readonly List<Type> _entityTypes = new();
    private ConfigManager _configManager;

    public void Initialize(ConfigManager configManager)
    {
        _configManager = configManager;
    }

    public void RegisterEntity<T>() where T : new()
    {
        _entityTypes.Add(typeof(T));
    }

    public void Load()
    {
        var config = _configManager.GetConfig<DatabaseConfig>();
        _connectionString = new MySqlConnectionStringBuilder
        {
            Server = config.Host,
            Port = config.Port,
            Database = config.Database,
            UserID = config.Username,
            Password = config.Password
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

    public void Unload()
    {
        _dbSets.Clear();
    }

    public DbSet<T> Table<T>() where T : new()
    {
        return (DbSet<T>)_dbSets.GetOrAdd(typeof(T), _ => new DbSet<T>(CreateConnection));
    }

    public MySqlConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}

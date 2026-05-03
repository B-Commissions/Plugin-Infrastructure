using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private readonly List<(Type Type, MigrationMode Mode)> _entityTypes = [];
    private DatabaseConfig _config;

    public void Initialize(ConfigManager configManager) =>
        _config = configManager.GetConfig<DatabaseConfig>();

    public void Initialize(DatabaseConfig config) =>
        _config = config;

    /// <summary>
    /// Register an entity for schema sync.
    /// </summary>
    /// <param name="migration">
    /// How to handle existing tables. Default <see cref="MigrationMode.None"/> only creates
    /// missing tables; <see cref="MigrationMode.Update"/> additively migrates schema; 
    /// <see cref="MigrationMode.Reset"/> drops and recreates (dev only).
    /// </param>
    /// <remarks>
    /// Register parent entities before children when foreign keys are involved — inline
    /// FK constraints require the referenced table to exist at CREATE time.
    /// </remarks>
    public void RegisterEntity<T>(MigrationMode migration = MigrationMode.None) where T : new()
    {
        _entityTypes.Add((typeof(T), migration));
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
        ThreadHelper.RunAsynchronously(async () =>
        {
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                foreach (var (type, mode) in _entityTypes)
                {
                    var metadata = TableMetadata.For(type);
                    await Migrator.ApplyAsync(conn, metadata, mode);
                }

                Logger.Log("[Database] Schema sync complete.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "[Database] Failed to sync schema.");
            }
        });
    }

    public void Unload() => _dbSets.Clear();

    public DbSet<T> Table<T>() where T : new() =>
        (DbSet<T>)_dbSets.GetOrAdd(typeof(T), _ => new DbSet<T>(CreateConnection));

    /// <summary>
    /// Create a new MySQL connection. Caller is responsible for opening, using, and disposing it.
    /// Use this for genuinely arbitrary SQL that doesn't fit <see cref="DbSet{T}.QuerySqlAsync"/>
    /// or <see cref="DbSet{T}.ExecuteSqlAsync"/>.
    /// </summary>
    public MySqlConnection CreateConnection() => new(_connectionString);

    /// <summary>
    /// Convenience wrapper for transactional or multi-statement work that needs one connection.
    /// </summary>
    public async Task<TResult> WithConnectionAsync<TResult>(Func<MySqlConnection, Task<TResult>> action)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        return await action(conn);
    }
}

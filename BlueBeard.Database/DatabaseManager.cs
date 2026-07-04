using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueBeard.Core;
using BlueBeard.Core.Configs;
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
        // Block the caller until the schema is fully synced, so every table exists before
        // any consumer queries it. Run via Task.Run so the async DB continuations resume on
        // the thread pool (no captured SynchronizationContext) and GetResult() can't deadlock
        // the RocketMod/Unity main thread.
        try
        {
            Task.Run(async () =>
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();

                foreach (var (type, mode) in _entityTypes)
                {
                    var metadata = TableMetadata.For(type);
                    await Migrator.ApplyAsync(conn, metadata, mode);
                }

                Logger.Log("[Database] Schema sync complete.");
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Log loudly, then rethrow — a missing schema must fail startup rather than
            // silently leave tables uncreated and surface later as "table doesn't exist".
            Logger.LogException(ex, "[Database] Failed to sync schema.");
            throw;
        }
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
    /// Convenience wrapper for multi-statement work that needs one open connection.
    /// This does NOT start a transaction — use <see cref="BeginTransactionAsync"/> for atomicity.
    /// </summary>
    public async Task<TResult> WithConnectionAsync<TResult>(Func<MySqlConnection, Task<TResult>> action)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        return await action(conn);
    }

    /// <summary>
    /// Open a connection and begin a transaction. Pass the result to the DbSet overloads
    /// that accept a <see cref="BbTransaction"/>; dispose without committing to roll back.
    /// </summary>
    public async Task<BbTransaction> BeginTransactionAsync()
    {
        var conn = CreateConnection();
        await conn.OpenAsync();
        try
        {
            var tx = await conn.BeginTransactionAsync();
            return new BbTransaction(conn, tx);
        }
        catch
        {
            conn.Dispose();
            throw;
        }
    }
}

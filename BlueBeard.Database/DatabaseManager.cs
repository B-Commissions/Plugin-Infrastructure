using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    private readonly List<IMigration> _migrations = [];
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

    /// <summary>
    /// Register a versioned run-once migration (renames, backfills, destructive changes —
    /// anything additive schema sync can't express). Pending migrations run in ascending
    /// version order during Load, after schema sync, tracked in __bluebeard_migrations.
    /// </summary>
    public void RegisterMigration(IMigration migration)
    {
        _migrations.Add(migration);
    }

    public void Load()
    {
        BuildConnectionString();
        SyncSchema();
    }

    /// <summary>
    /// Fully async variant of <see cref="Load"/> for consumers that keep startup off the
    /// main thread. The sync Load is unchanged and blocks until the schema exists.
    /// </summary>
    public async Task LoadAsync()
    {
        BuildConnectionString();
        try
        {
            await SyncSchemaCoreAsync();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "[Database] Failed to sync schema.");
            throw;
        }
    }

    private void BuildConnectionString()
    {
        _connectionString = new MySqlConnectionStringBuilder
        {
            Server = _config.Host,
            Port = _config.Port,
            Database = _config.Database,
            UserID = _config.Username,
            Password = _config.Password
        }.ConnectionString;
    }

    public void SyncSchema()
    {
        // Block the caller until the schema is fully synced, so every table exists before
        // any consumer queries it. Run via Task.Run so the async DB continuations resume on
        // the thread pool (no captured SynchronizationContext) and GetResult() can't deadlock
        // the RocketMod/Unity main thread.
        try
        {
            Task.Run(SyncSchemaCoreAsync).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Log loudly, then rethrow — a missing schema must fail startup rather than
            // silently leave tables uncreated and surface later as "table doesn't exist".
            Logger.LogException(ex, "[Database] Failed to sync schema.");
            throw;
        }
    }

    private async Task SyncSchemaCoreAsync()
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();

        foreach (var (type, mode) in _entityTypes)
        {
            var metadata = TableMetadata.For(type);
            await Migrator.ApplyAsync(conn, metadata, mode);
        }

        await RunMigrationsAsync(conn);

        Logger.Log("[Database] Schema sync complete.");
    }

    private async Task RunMigrationsAsync(MySqlConnection conn)
    {
        if (_migrations.Count == 0) return;

        using (var create = new MySqlCommand(
            "CREATE TABLE IF NOT EXISTS `__bluebeard_migrations` (" +
            "`version` INT PRIMARY KEY, `applied_at` DATETIME NOT NULL);", conn))
        {
            await create.ExecuteNonQueryAsync();
        }

        var applied = new HashSet<int>();
        using (var select = new MySqlCommand("SELECT `version` FROM `__bluebeard_migrations`;", conn))
        using (var reader = await select.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                applied.Add(reader.GetInt32(0));
        }

        var duplicate = _migrations.GroupBy(m => m.Version).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            throw new InvalidOperationException(
                $"[Database] Two migrations registered with version {duplicate.Key}.");

        foreach (var migration in _migrations.OrderBy(m => m.Version))
        {
            if (applied.Contains(migration.Version)) continue;

            Logger.Log($"[Database] Applying migration v{migration.Version} ({migration.GetType().Name})...");
            await migration.UpAsync(conn);

            using var record = new MySqlCommand(
                "INSERT INTO `__bluebeard_migrations` (`version`, `applied_at`) VALUES (@v, UTC_TIMESTAMP());", conn);
            record.Parameters.AddWithValue("@v", migration.Version);
            await record.ExecuteNonQueryAsync();

            Logger.Log($"[Database] Migration v{migration.Version} applied.");
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

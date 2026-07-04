using System;
using BlueBeard.Core.Helpers;
using BlueBeard.Database;

namespace BlueBeard.Cooldowns;

/// <summary>
/// Cooldown manager that additionally persists each cooldown to a MySQL table via
/// <see cref="BlueBeard.Database.DatabaseManager"/> so cooldowns survive server restarts.
///
/// Use this only when cooldowns MUST outlive the process. Most gameplay cooldowns are
/// ephemeral and should use the base <see cref="CooldownManager"/>.
///
/// Workflow:
/// <code>
/// db.RegisterEntity&lt;BBCooldownRow&gt;();
/// db.Load();                   // schema sync ensures bb_cooldowns exists
/// var cooldowns = new PersistentCooldownManager();
/// cooldowns.Initialize(db);
/// cooldowns.Load();            // reads unexpired rows into memory
/// </code>
///
/// DB writes are fire-and-forget via <see cref="ThreadHelper.RunAsynchronously(Action, string)"/>
/// so <c>Start</c> remains synchronous from the caller's perspective.
/// </summary>
public class PersistentCooldownManager : CooldownManager
{
    private DatabaseManager _db;

    public PersistentCooldownManager() : base() { }

    public PersistentCooldownManager(Func<DateTime> utcNow) : base(utcNow) { }

    public void Initialize(DatabaseManager database)
    {
        _db = database;
    }

    public override void Start(string key, float durationSeconds)
    {
        base.Start(key, durationSeconds);
        PersistStart(key);
    }

    public override void Start(string key, TimeSpan duration)
    {
        base.Start(key, duration);
        PersistStart(key);
    }

    public override void Cancel(string key)
    {
        base.Cancel(key);
        if (_db == null) return;
        ThreadHelper.RunAsynchronously(async () =>
        {
            await _db.Table<BBCooldownRow>().DeleteAsync(r => r.Key == key);
        }, "[Cooldowns] Failed to delete cooldown row.");
    }

    public override void CancelByPrefix(string prefix)
    {
        // Snapshot matching keys BEFORE clearing so we can delete each DB row individually.
        // (BlueBeard.Database's expression visitor does not support StartsWith / CompareTo.)
        var matchingKeys = new System.Collections.Generic.List<string>();
        foreach (var key in GetKeysSnapshot())
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
                matchingKeys.Add(key);
        }

        base.CancelByPrefix(prefix);

        if (_db == null || matchingKeys.Count == 0) return;
        ThreadHelper.RunAsynchronously(async () =>
        {
            var table = _db.Table<BBCooldownRow>();
            foreach (var key in matchingKeys)
                await table.DeleteAsync(r => r.Key == key);
        }, "[Cooldowns] Failed to delete cooldowns by prefix.");
    }

    /// <summary>
    /// Loads all unexpired cooldowns from the database into memory. Should be called
    /// after <see cref="Initialize"/> and after the DatabaseManager has finished its
    /// schema sync (i.e. after <see cref="DatabaseManager.Load"/>).
    /// </summary>
    public override void Load()
    {
        base.Load();
        if (_db == null) return;

        ThreadHelper.RunAsynchronously(async () =>
        {
            var table = _db.Table<BBCooldownRow>();
            var now = UtcNow;

            // Expired rows were previously never removed, growing bb_cooldowns forever.
            await table.DeleteAsync(r => r.Expiry <= now);

            var rows = await table.Where(r => r.Expiry > now);
            ThreadHelper.RunSynchronously(() =>
            {
                foreach (var row in rows)
                {
                    // Use base.Start so we don't re-persist rows we just loaded.
                    var remaining = row.Expiry - UtcNow;
                    if (remaining > TimeSpan.Zero)
                        base.Start(row.Key, remaining);
                }
            });
        }, "[Cooldowns] Failed to load cooldowns from database.");
    }

    public override void Unload()
    {
        base.Unload();
        // Rows remain in the database intentionally — they are what lets state survive restarts.
    }

    private void PersistStart(string key)
    {
        if (_db == null) return;
        var remainingSeconds = GetRemaining(key);
        if (remainingSeconds <= 0f) return;
        var expiry = UtcNow.AddSeconds(remainingSeconds);

        ThreadHelper.RunAsynchronously(async () =>
        {
            // Atomic upsert — the old delete-then-insert pair could interleave between two
            // rapid Start() calls for the same key (PK collision or lost row).
            await _db.Table<BBCooldownRow>().ExecuteSqlAsync(
                "INSERT INTO `bb_cooldowns` (`cooldown_key`, `expiry`) VALUES (@key, @expiry) " +
                "ON DUPLICATE KEY UPDATE `expiry` = @expiry;",
                ("@key", key), ("@expiry", expiry));
        }, "[Cooldowns] Failed to persist cooldown row.");
    }
}

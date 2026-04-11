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
            var now = DateTime.UtcNow;
            var rows = await _db.Table<BBCooldownRow>().Where(r => r.Expiry > now);
            ThreadHelper.RunSynchronously(() =>
            {
                foreach (var row in rows)
                {
                    // Use base.Start so we don't re-persist rows we just loaded.
                    var remaining = row.Expiry - DateTime.UtcNow;
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
        var expiry = DateTime.UtcNow.AddSeconds(remainingSeconds);

        ThreadHelper.RunAsynchronously(async () =>
        {
            var table = _db.Table<BBCooldownRow>();
            // Overwrite: delete then insert. A proper upsert requires raw SQL which DbSet doesn't expose.
            await table.DeleteAsync(r => r.Key == key);
            await table.InsertAsync(new BBCooldownRow { Key = key, Expiry = expiry });
        }, "[Cooldowns] Failed to persist cooldown row.");
    }
}

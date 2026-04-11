# Persistence

`PersistentCooldownManager` extends `CooldownManager` with MySQL persistence via `BlueBeard.Database`. Use this when cooldowns must survive a server restart.

Most cooldowns are ephemeral and should use the base `CooldownManager`. Reach for the persistent variant only when:

- The cooldown represents a meaningful gameplay penalty that shouldn't be bypassed by a restart (e.g. faction disband lockout, raid timer).
- The expiry window is long enough that a restart during that window is plausible (hours or days).
- Losing the cooldown on restart would be exploited by players.

## The BBCooldownRow entity

Rows are stored in the `bb_cooldowns` table:

```csharp
[Table("bb_cooldowns")]
public class BBCooldownRow
{
    [PrimaryKey]
    [Column("cooldown_key")]
    [ColumnType("VARCHAR(191)")]
    public string Key { get; set; }

    [Column("expiry")]
    [ColumnType("DATETIME")]
    public DateTime Expiry { get; set; }
}
```

The key column is the same string the caller passes to `Start` / `IsActive`. The 191-byte length is the MySQL/utf8mb4 primary-key safe default.

## Setup

```csharp
using BlueBeard.Cooldowns;
using BlueBeard.Database;

public class MyPlugin : RocketPlugin
{
    public static DatabaseManager Database { get; private set; }
    public static PersistentCooldownManager Cooldowns { get; private set; }

    protected override void Load()
    {
        // 1. Initialise the database (schema sync happens here).
        Database = new DatabaseManager();
        Database.Initialize(Configuration.Instance);
        Database.RegisterEntity<BBCooldownRow>();
        Database.Load();

        // 2. Initialise the persistent cooldown manager AFTER the database is live.
        Cooldowns = new PersistentCooldownManager();
        Cooldowns.Initialize(Database);
        Cooldowns.Load();   // loads unexpired rows from bb_cooldowns back into memory
    }

    protected override void Unload()
    {
        Cooldowns.Unload();
        Database.Unload();
    }
}
```

## Behaviour

### Start
Writes the expiry to memory (fast, synchronous) and asynchronously deletes any existing row for the same key then inserts a new one. The caller never blocks on the DB write.

### IsActive / GetRemaining
Reads from in-memory state only, identical to the base class. The DB row is authoritative only at `Load` time.

### Cancel / CancelByPrefix
Removes from memory immediately and asynchronously deletes the corresponding rows.

### Load
Reads every unexpired row from the database (`expiry > now`) and replays them into memory. After this, `IsActive` lookups see both the pre-existing persisted cooldowns and any new ones started in the current process.

### Unload
Clears the in-memory state. DB rows are intentionally NOT deleted -- their purpose is to survive the process.

## Threading

DB writes are fire-and-forget via `ThreadHelper.RunAsynchronously`, so the public API stays synchronous. If a write fails, the exception is logged but the in-memory state still reflects the `Start` / `Cancel` call. This is the same consistency model the rest of `BlueBeard.Database` uses.

## Caveats

- **CancelByPrefix** iterates in-memory keys before clearing and deletes each matching DB row individually. `BlueBeard.Database`'s expression visitor does not support `StartsWith` or string `CompareTo`, so prefix queries can't be expressed as a single DELETE statement. For domains with thousands of cooldowns this can produce a lot of round trips; keep `CancelByPrefix` usage moderate.
- **Upsert** is implemented as "DELETE then INSERT" because `BlueBeard.Database` doesn't expose a native upsert. For a high-frequency `Start` of the same key this means two round trips per write.
- **Clock skew** between the server clock and the database clock affects the `Load` filter (`expiry > now`). The manager uses the process UTC clock, so if the DB is in a different timezone the rows still sort correctly but relative expiry may drift by the skew amount.

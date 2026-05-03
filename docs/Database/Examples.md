# Examples

This page shows complete plugin implementations using BlueBeard.Database.

---

## Entity Definitions

### Faction (with HasMany)

```csharp
using System;
using System.Collections.Generic;
using BlueBeard.Database.Attributes;

[Table("factions")]
public class Faction
{
    [PrimaryKey]
    [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("public_id")]
    public Guid PublicId { get; set; }   // CHAR(36) via auto-registered GuidConverter

    [Column("name")]
    public string Name { get; set; }

    [Column("leader_steam_id")]
    public ulong LeaderSteamId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("level")]
    public int Level { get; set; }

    [Column("status")]
    public FactionStatus Status { get; set; }

    [ColumnType("TEXT")]
    [Column("description")]
    public string Description { get; set; }

    // Auto-populated when a Faction is loaded
    [HasMany(nameof(Member.FactionId))]
    public List<Member> Members { get; set; }
}

public enum FactionStatus
{
    Active = 0,
    Inactive = 1,
    Disbanded = 2
}
```

### Member (with ForeignKey + BelongsTo)

```csharp
using System;
using BlueBeard.Database.Attributes;

[Table("members")]
public class Member
{
    [PrimaryKey]
    [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("faction_id")]
    [ForeignKey(typeof(Faction), nameof(Faction.Id), OnDelete = ReferentialAction.Cascade)]
    public int FactionId { get; set; }

    // Auto-populated when a Member is loaded
    [BelongsTo(nameof(FactionId))]
    public Faction Faction { get; set; }

    [Column("steam_id")]
    public ulong SteamId { get; set; }

    [Column("role")]
    public MemberRole Role { get; set; }

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }
}

public enum MemberRole
{
    Member = 0,
    Officer = 1,
    Leader = 2
}
```

---

## Plugin Setup

```csharp
using BlueBeard.Core.Configs;
using BlueBeard.Core.Helpers;
using BlueBeard.Database;
using Rocket.API;
using Rocket.Core.Plugins;

public class FactionPlugin : RocketPlugin
{
    public static FactionPlugin Instance { get; private set; }
    public DatabaseManager DatabaseManager { get; private set; }

    private ConfigManager _configManager;

    protected override void Load()
    {
        Instance = this;

        _configManager = new ConfigManager();
        _configManager.Initialize(Directory);
        _configManager.LoadConfig<DatabaseConfig>();

        DatabaseManager = new DatabaseManager();
        DatabaseManager.Initialize(_configManager);

        // Parent first (required when Member has a FK to Faction).
        // MigrationMode.Update lets us evolve schema during active development.
        DatabaseManager.RegisterEntity<Faction>(MigrationMode.Update);
        DatabaseManager.RegisterEntity<Member>(MigrationMode.Update);

        DatabaseManager.Load();

        Rocket.Core.Logging.Logger.Log("FactionPlugin loaded.");
    }

    protected override void Unload()
    {
        DatabaseManager.Unload();
        Rocket.Core.Logging.Logger.Log("FactionPlugin unloaded.");
    }
}
```

---

## Command: Create Faction

```csharp
using System;
using System.Collections.Generic;
using BlueBeard.Core.Helpers;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

public class CreateFactionCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "createfaction";
    public string Help => "Creates a new faction.";
    public string Syntax => "<name>";
    public List<string> Aliases => new();
    public List<string> Permissions => new() { "faction.create" };

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 1)
        {
            UnturnedChat.Say(caller, "Usage: /createfaction <name>");
            return;
        }

        var player = (UnturnedPlayer)caller;
        var factionName = command[0];
        var steamId = player.CSteamID.m_SteamID;

        ThreadHelper.RunAsynchronously(async () =>
        {
            var factions = FactionPlugin.Instance.DatabaseManager.Table<Faction>();

            var existing = await factions.FirstOrDefaultAsync(f => f.Name == factionName);
            if (existing != null)
            {
                ThreadHelper.RunSynchronously(() =>
                    UnturnedChat.Say(caller, $"A faction named '{factionName}' already exists."));
                return;
            }

            var faction = new Faction
            {
                PublicId = Guid.NewGuid(),
                Name = factionName,
                LeaderSteamId = steamId,
                CreatedAt = DateTime.UtcNow,
                Level = 1,
                Status = FactionStatus.Active
            };
            await factions.InsertAsync(faction);

            var members = FactionPlugin.Instance.DatabaseManager.Table<Member>();
            await members.InsertAsync(new Member
            {
                FactionId = faction.Id,  // populated by InsertAsync
                SteamId = steamId,
                Role = MemberRole.Leader,
                JoinedAt = DateTime.UtcNow
            });

            ThreadHelper.RunSynchronously(() =>
                UnturnedChat.Say(caller, $"Faction '{factionName}' created (ID: {faction.Id})."));
        }, "Failed to create faction");
    }
}
```

---

## Command: Show Faction Roster

This shows the navigation property in action. Loading the faction also loads its members in a single follow-up query.

```csharp
using System.Collections.Generic;
using BlueBeard.Core.Helpers;
using Rocket.API;
using Rocket.Unturned.Chat;

public class FactionRosterCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "factionroster";
    public string Help => "Shows the roster of a faction.";
    public string Syntax => "<name>";
    public List<string> Aliases => new();
    public List<string> Permissions => new() { "faction.view" };

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 1) { UnturnedChat.Say(caller, "Usage: /factionroster <name>"); return; }
        var name = command[0];

        ThreadHelper.RunAsynchronously(async () =>
        {
            var factions = FactionPlugin.Instance.DatabaseManager.Table<Faction>();
            var faction = await factions.FirstOrDefaultAsync(f => f.Name == name);

            ThreadHelper.RunSynchronously(() =>
            {
                if (faction == null) { UnturnedChat.Say(caller, "Faction not found."); return; }

                UnturnedChat.Say(caller, $"=== {faction.Name} (Level {faction.Level}) ===");
                // faction.Members was loaded automatically via [HasMany]
                foreach (var m in faction.Members)
                {
                    UnturnedChat.Say(caller, $"  {m.Role}: {m.SteamId}");
                }
                UnturnedChat.Say(caller, $"Total: {faction.Members.Count}");
            });
        }, "Failed to load faction roster");
    }
}
```

---

## Command: Find My Faction (BelongsTo example)

Loading a member auto-populates `member.Faction`, so you can show parent info without a second explicit query.

```csharp
using System.Collections.Generic;
using BlueBeard.Core.Helpers;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

public class MyFactionCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "myfaction";
    public string Help => "Shows your current faction.";
    public string Syntax => "";
    public List<string> Aliases => new();
    public List<string> Permissions => new() { "faction.view" };

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = (UnturnedPlayer)caller;
        var steamId = player.CSteamID.m_SteamID;

        ThreadHelper.RunAsynchronously(async () =>
        {
            var members = FactionPlugin.Instance.DatabaseManager.Table<Member>();
            var membership = await members.FirstOrDefaultAsync(m => m.SteamId == steamId);

            ThreadHelper.RunSynchronously(() =>
            {
                if (membership == null) { UnturnedChat.Say(caller, "You're not in a faction."); return; }

                // membership.Faction was loaded automatically via [BelongsTo]
                UnturnedChat.Say(caller,
                    $"You are a {membership.Role} of {membership.Faction.Name} " +
                    $"(joined {membership.JoinedAt:yyyy-MM-dd}).");
            });
        }, "Failed to load membership");
    }
}
```

---

## Command: Disband Faction

This example demonstrates `UpdateAsync` and `DeleteAsync` with an expression predicate. Note that `OnDelete = Cascade` on the FK means deleting a Faction would automatically cascade to Members at the database level — but here we mark the faction disbanded instead, and clear members manually.

```csharp
using System;
using System.Collections.Generic;
using BlueBeard.Core.Helpers;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

public class DisbandFactionCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "disbandfaction";
    public string Help => "Disbands your faction.";
    public string Syntax => "";
    public List<string> Aliases => new();
    public List<string> Permissions => new() { "faction.disband" };

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = (UnturnedPlayer)caller;
        var steamId = player.CSteamID.m_SteamID;

        ThreadHelper.RunAsynchronously(async () =>
        {
            var factions = FactionPlugin.Instance.DatabaseManager.Table<Faction>();
            var members = FactionPlugin.Instance.DatabaseManager.Table<Member>();

            var faction = await factions.FirstOrDefaultAsync(
                f => f.LeaderSteamId == steamId && f.Status == FactionStatus.Active);

            if (faction == null)
            {
                ThreadHelper.RunSynchronously(() =>
                    UnturnedChat.Say(caller, "You are not the leader of any active faction."));
                return;
            }

            faction.Status = FactionStatus.Disbanded;
            await factions.UpdateAsync(faction);

            int factionId = faction.Id;
            await members.DeleteAsync(m => m.FactionId == factionId);

            ThreadHelper.RunSynchronously(() =>
                UnturnedChat.Say(caller, $"Faction '{faction.Name}' has been disbanded."));
        }, "Failed to disband faction");
    }
}
```

---

## Custom converter example

Storing a CLR type that doesn't have a built-in converter — say, a `Vector3` from UnityEngine, packed as `"x,y,z"` in a `VARCHAR`:

```csharp
using System;
using BlueBeard.Database.Converters;
using UnityEngine;

public class Vector3Converter : IValueConverter
{
    public Type ClrType => typeof(Vector3);
    public string DefaultSqlType => "VARCHAR(64)";

    public object ToProvider(object clrValue)
    {
        var v = (Vector3)clrValue;
        return $"{v.x},{v.y},{v.z}";
    }

    public object FromProvider(object providerValue)
    {
        var s = providerValue as string ?? providerValue?.ToString();
        if (string.IsNullOrEmpty(s)) return Vector3.zero;
        var parts = s.Split(',');
        return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
    }
}

// Register once at plugin startup, before any entity is queried:
ValueConverters.Register(new Vector3Converter());
```

After registration, a `public Vector3 SpawnPosition { get; set; }` property on any entity will Just Work — schema gets `VARCHAR(64)`, inserts and reads round-trip cleanly, and `Where(e => e.SpawnPosition == knownPosition)` parameter-binds through the converter.

---

## Summary

The typical workflow for any database operation in a command:

1. Call `ThreadHelper.RunAsynchronously(async () => { ... })` to move off the main thread.
2. Get your `DbSet<T>` via `FactionPlugin.Instance.DatabaseManager.Table<T>()`.
3. Perform async CRUD operations (`QueryAsync`, `Where`, `FirstOrDefaultAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`).
4. Call `ThreadHelper.RunSynchronously(() => { ... })` to dispatch UI updates and Unturned API calls back to the main thread.
5. Pass an error message string as the second argument to `RunAsynchronously` so failures are logged.

For arbitrary SQL the expression visitor can't translate (joins, `LIKE`, subqueries), use `QuerySqlAsync` or `WithConnectionAsync` — see [Queries](Queries).
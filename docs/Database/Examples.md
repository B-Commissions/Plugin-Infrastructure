# Examples

This page shows a complete plugin implementation that uses BlueBeard.Database for persistent faction and member management.

---

## Entity Definitions

### Faction

```csharp
using System;
using BlueBeard.Database.Attributes;

[Table("factions")]
public class Faction
{
    [PrimaryKey]
    [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

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
}

public enum FactionStatus
{
    Active = 0,
    Inactive = 1,
    Disbanded = 2
}
```

### Member

```csharp
using System;
using BlueBeard.Database.Attributes;

[Table("faction_members")]
public class Member
{
    [PrimaryKey]
    [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("faction_id")]
    public int FactionId { get; set; }

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

        // Config
        _configManager = new ConfigManager();
        _configManager.Initialize(Directory);
        _configManager.LoadConfig<DatabaseConfig>();

        // Database
        DatabaseManager = new DatabaseManager();
        DatabaseManager.Initialize(_configManager);
        DatabaseManager.RegisterEntity<Faction>();
        DatabaseManager.RegisterEntity<Member>();
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

            // Check if name is taken
            var existing = await factions.FirstOrDefaultAsync(f => f.Name == factionName);
            if (existing != null)
            {
                ThreadHelper.RunSynchronously(() =>
                    UnturnedChat.Say(caller, $"A faction named '{factionName}' already exists."));
                return;
            }

            // Create the faction
            var faction = new Faction
            {
                Name = factionName,
                LeaderSteamId = steamId,
                CreatedAt = DateTime.UtcNow,
                Level = 1,
                Status = FactionStatus.Active
            };
            await factions.InsertAsync(faction);

            // Add the creator as leader
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

## Command: List Factions

```csharp
using System.Collections.Generic;
using BlueBeard.Core.Helpers;
using Rocket.API;
using Rocket.Unturned.Chat;

public class ListFactionsCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "listfactions";
    public string Help => "Lists all active factions.";
    public string Syntax => "";
    public List<string> Aliases => new();
    public List<string> Permissions => new() { "faction.list" };

    public void Execute(IRocketPlayer caller, string[] command)
    {
        ThreadHelper.RunAsynchronously(async () =>
        {
            var factions = FactionPlugin.Instance.DatabaseManager.Table<Faction>();
            var list = await factions.Where(f => f.Status == FactionStatus.Active);

            ThreadHelper.RunSynchronously(() =>
            {
                if (list.Count == 0)
                {
                    UnturnedChat.Say(caller, "No active factions.");
                    return;
                }

                UnturnedChat.Say(caller, $"Active factions ({list.Count}):");
                foreach (var f in list)
                {
                    UnturnedChat.Say(caller, $"  [{f.Id}] {f.Name} - Level {f.Level}");
                }
            });
        }, "Failed to list factions");
    }
}
```

---

## Command: Add Member

```csharp
using System;
using System.Collections.Generic;
using BlueBeard.Core.Helpers;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Steamworks;

public class AddMemberCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "factioninvite";
    public string Help => "Adds a player to your faction.";
    public string Syntax => "<player>";
    public List<string> Aliases => new();
    public List<string> Permissions => new() { "faction.invite" };

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 1)
        {
            UnturnedChat.Say(caller, "Usage: /factioninvite <player>");
            return;
        }

        var player = (UnturnedPlayer)caller;
        var target = UnturnedPlayer.FromName(command[0]);
        if (target == null)
        {
            UnturnedChat.Say(caller, "Player not found.");
            return;
        }

        var callerSteamId = player.CSteamID.m_SteamID;
        var targetSteamId = target.CSteamID.m_SteamID;

        ThreadHelper.RunAsynchronously(async () =>
        {
            var factions = FactionPlugin.Instance.DatabaseManager.Table<Faction>();
            var members = FactionPlugin.Instance.DatabaseManager.Table<Member>();

            // Find the caller's faction (must be leader)
            var faction = await factions.FirstOrDefaultAsync(
                f => f.LeaderSteamId == callerSteamId && f.Status == FactionStatus.Active);

            if (faction == null)
            {
                ThreadHelper.RunSynchronously(() =>
                    UnturnedChat.Say(caller, "You are not the leader of any active faction."));
                return;
            }

            // Check if target is already a member
            var existingMember = await members.FirstOrDefaultAsync(
                m => m.FactionId == faction.Id && m.SteamId == targetSteamId);

            if (existingMember != null)
            {
                ThreadHelper.RunSynchronously(() =>
                    UnturnedChat.Say(caller, $"{target.DisplayName} is already in your faction."));
                return;
            }

            // Add the member
            await members.InsertAsync(new Member
            {
                FactionId = faction.Id,
                SteamId = targetSteamId,
                Role = MemberRole.Member,
                JoinedAt = DateTime.UtcNow
            });

            ThreadHelper.RunSynchronously(() =>
            {
                UnturnedChat.Say(caller, $"{target.DisplayName} has been added to {faction.Name}.");
                UnturnedChat.Say(target, $"You have been added to faction {faction.Name}.");
            });
        }, "Failed to add member");
    }
}
```

---

## Command: Disband Faction

This example demonstrates `UpdateAsync` and `DeleteAsync` with an expression predicate.

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

            // Find faction where caller is leader
            var faction = await factions.FirstOrDefaultAsync(
                f => f.LeaderSteamId == steamId && f.Status == FactionStatus.Active);

            if (faction == null)
            {
                ThreadHelper.RunSynchronously(() =>
                    UnturnedChat.Say(caller, "You are not the leader of any active faction."));
                return;
            }

            // Mark the faction as disbanded
            faction.Status = FactionStatus.Disbanded;
            await factions.UpdateAsync(faction);

            // Remove all members using expression-based delete
            int factionId = faction.Id;
            await members.DeleteAsync(m => m.FactionId == factionId);

            ThreadHelper.RunSynchronously(() =>
                UnturnedChat.Say(caller, $"Faction '{faction.Name}' has been disbanded."));
        }, "Failed to disband faction");
    }
}
```

---

## Summary

The typical workflow for any database operation in a command:

1. Call `ThreadHelper.RunAsynchronously(async () => { ... })` to move off the main thread.
2. Get your `DbSet<T>` via `FactionPlugin.Instance.DatabaseManager.Table<T>()`.
3. Perform async CRUD operations (`QueryAsync`, `Where`, `FirstOrDefaultAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`).
4. Call `ThreadHelper.RunSynchronously(() => { ... })` to dispatch UI updates and Unturned API calls back to the main thread.
5. Pass an error message string as the second argument to `RunAsynchronously` so failures are logged.

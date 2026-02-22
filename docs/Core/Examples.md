# Full Implementation Examples

This page shows a complete example plugin that uses BlueBeard.Core's configuration system, command framework, threading helpers, and messaging utilities.

## Example Plugin: BountySystem

A plugin that lets players place bounties on each other. It demonstrates:

- A custom config with `IConfig` and `ConfigManager`
- A command tree with nested `SubCommandGroup`
- Using `ThreadHelper` for async database work
- Using `MessageHelper` from a background thread

### Project Structure

```
BountyPlugin/
  BountyPlugin.cs           -- Main plugin class
  BountyConfig.cs           -- Configuration
  BountyDatabase.cs         -- Simulated database layer
  Commands/
    BountyCommand.cs         -- Top-level /bounty command
    BountySetCommand.cs      -- /bounty set <player> <amount>
    BountyListCommand.cs     -- /bounty list
    BountyAdminResetCommand.cs   -- /bounty admin reset <player>
    BountyAdminReloadCommand.cs  -- /bounty admin reload
```

### BountyConfig.cs

```csharp
using System.Collections.Generic;
using BlueBeard.Core.Configs;

namespace BountyPlugin;

public class BountyConfig : IConfig
{
    public int MinBounty { get; set; }
    public int MaxBounty { get; set; }
    public float TaxRate { get; set; }
    public string BountySetMessage { get; set; }
    public string BountyClaimedMessage { get; set; }
    public List<string> BlockedMaps { get; set; }

    public void LoadDefaults()
    {
        MinBounty = 50;
        MaxBounty = 100000;
        TaxRate = 0.05f;
        BountySetMessage = "{player} placed a ${amount} bounty on {target}!";
        BountyClaimedMessage = "{killer} claimed the ${amount} bounty on {victim}!";
        BlockedMaps = new List<string> { "Arena", "Tutorial" };
    }
}
```

### BountyDatabase.cs

A simulated async database layer. In a real plugin, this would use MySQL, LiteDB, or another data store.

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;

namespace BountyPlugin;

public class BountyDatabase
{
    private readonly Dictionary<ulong, int> _bounties = new();

    public Task SetBounty(CSteamID target, int amount)
    {
        _bounties[target.m_SteamID] = amount;
        return Task.Delay(50); // Simulate async I/O
    }

    public Task<int> GetBounty(CSteamID target)
    {
        _bounties.TryGetValue(target.m_SteamID, out var amount);
        return Task.FromResult(amount);
    }

    public Task<Dictionary<ulong, int>> GetAllBounties()
    {
        return Task.FromResult(new Dictionary<ulong, int>(_bounties));
    }

    public Task ResetBounty(CSteamID target)
    {
        _bounties.Remove(target.m_SteamID);
        return Task.Delay(50);
    }
}
```

### BountyCommand.cs (Top-Level Command)

```csharp
using System.Collections.Generic;
using BlueBeard.Core.Commands;
using BountyPlugin.Commands;
using Rocket.API;

namespace BountyPlugin;

public class BountyCommand : CommandBase
{
    public override AllowedCaller AllowedCaller => AllowedCaller.Both;
    public override string Name => "bounty";
    public override string Help => "Manage player bounties";
    public override string Syntax => "<set | list | admin>";
    public override List<string> Aliases => new() { "b" };
    public override List<string> Permissions => new() { "bounty" };

    public override SubCommand[] Children => new SubCommand[]
    {
        new BountySetCommand(),
        new BountyListCommand(),
        new SubCommandGroup("admin", new[] { "a" }, "bounty.admin", new SubCommand[]
        {
            new BountyAdminResetCommand(),
            new BountyAdminReloadCommand(),
        }),
    };
}
```

### BountySetCommand.cs

Demonstrates using `ThreadHelper` to run async database work off the main thread and `MessageHelper` to send messages from the background thread.

```csharp
using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using BlueBeard.Core.Helpers;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BountyPlugin.Commands;

public class BountySetCommand : SubCommand
{
    public override string Name => "set";
    public override string[] Aliases => new[] { "s", "place" };
    public override string Permission => "bounty.set";
    public override string Help => "Place a bounty on a player";
    public override string Syntax => "<player> <amount>";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 2)
        {
            CommandBase.Reply(caller, "Usage: /bounty set <player> <amount>", Color.yellow);
            return Task.CompletedTask;
        }

        var targetName = args[0];
        if (!int.TryParse(args[1], out var amount))
        {
            CommandBase.Reply(caller, "Amount must be a number.", Color.red);
            return Task.CompletedTask;
        }

        var config = BountyPlugin.Instance.ConfigManager.GetConfig<BountyConfig>();
        if (amount < config.MinBounty || amount > config.MaxBounty)
        {
            CommandBase.Reply(caller, $"Amount must be between {config.MinBounty} and {config.MaxBounty}.", Color.red);
            return Task.CompletedTask;
        }

        var target = PlayerTool.getPlayer(targetName);
        if (target == null)
        {
            CommandBase.Reply(caller, $"Player '{targetName}' not found.", Color.red);
            return Task.CompletedTask;
        }

        var targetSteamId = target.channel.owner.playerID.steamID;

        // Run the database operation on a background thread
        ThreadHelper.RunAsynchronously(async () =>
        {
            await BountyPlugin.Instance.Database.SetBounty(targetSteamId, amount);

            // MessageHelper is thread-safe, dispatches to main thread internally
            MessageHelper.Say(
                config.BountySetMessage
                    .Replace("{player}", caller.DisplayName)
                    .Replace("{amount}", amount.ToString())
                    .Replace("{target}", targetName),
                Color.yellow
            );

            MessageHelper.Say(caller, $"Bounty of ${amount} placed on {targetName}.", Color.green);
        }, "Failed to set bounty");

        return Task.CompletedTask;
    }
}
```

### BountyListCommand.cs

Demonstrates fetching data asynchronously and reporting results from a background thread.

```csharp
using System.Linq;
using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using BlueBeard.Core.Helpers;
using Rocket.API;
using UnityEngine;

namespace BountyPlugin.Commands;

public class BountyListCommand : SubCommand
{
    public override string Name => "list";
    public override string[] Aliases => new[] { "ls", "l" };
    public override string Permission => "bounty.list";
    public override string Help => "List all active bounties";
    public override string Syntax => "";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        // Run the database query on a background thread
        ThreadHelper.RunAsynchronously(async () =>
        {
            var bounties = await BountyPlugin.Instance.Database.GetAllBounties();

            if (bounties.Count == 0)
            {
                // Safe to call from background thread
                MessageHelper.Say(caller, "No active bounties.", Color.yellow);
                return;
            }

            MessageHelper.Say(caller, "=== Active Bounties ===", Color.cyan);
            foreach (var kvp in bounties.OrderByDescending(b => b.Value))
            {
                MessageHelper.Say(caller, $"  {kvp.Key}: ${kvp.Value}", Color.white);
            }
        }, "Failed to list bounties");

        return Task.CompletedTask;
    }
}
```

### BountyAdminResetCommand.cs

An admin-only sub-command nested inside the "admin" group.

```csharp
using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using BlueBeard.Core.Helpers;
using Rocket.API;
using SDG.Unturned;
using UnityEngine;

namespace BountyPlugin.Commands;

public class BountyAdminResetCommand : SubCommand
{
    public override string Name => "reset";
    public override string[] Aliases => new[] { "r", "clear" };
    public override string Permission => "bounty.admin.reset";
    public override string Help => "Reset a player's bounty";
    public override string Syntax => "<player>";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, "Usage: /bounty admin reset <player>", Color.yellow);
            return Task.CompletedTask;
        }

        var targetName = args[0];
        var target = PlayerTool.getPlayer(targetName);
        if (target == null)
        {
            CommandBase.Reply(caller, $"Player '{targetName}' not found.", Color.red);
            return Task.CompletedTask;
        }

        var targetSteamId = target.channel.owner.playerID.steamID;

        ThreadHelper.RunAsynchronously(async () =>
        {
            await BountyPlugin.Instance.Database.ResetBounty(targetSteamId);
            MessageHelper.Say(caller, $"Bounty on {targetName} has been reset.", Color.green);
        }, "Failed to reset bounty");

        return Task.CompletedTask;
    }
}
```

### BountyAdminReloadCommand.cs

An admin sub-command that reloads the config at runtime. This runs synchronously since it does not involve async I/O.

```csharp
using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

namespace BountyPlugin.Commands;

public class BountyAdminReloadCommand : SubCommand
{
    public override string Name => "reload";
    public override string Permission => "bounty.admin.reload";
    public override string Help => "Reload the bounty configuration";
    public override string Syntax => "";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        BountyPlugin.Instance.ConfigManager.ReloadConfig<BountyConfig>();
        CommandBase.Reply(caller, "Bounty configuration reloaded.", Color.green);
        return Task.CompletedTask;
    }
}
```

### BountyPlugin.cs (Main Plugin)

Ties everything together: initializes the config manager, loads configs, sets up the database, and generates command docs.

```csharp
using BlueBeard.Core.Configs;
using BlueBeard.Core.Helpers;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;

namespace BountyPlugin;

public class BountyPlugin : RocketPlugin
{
    public static BountyPlugin Instance { get; private set; }
    public ConfigManager ConfigManager { get; private set; }
    public BountyDatabase Database { get; private set; }

    protected override void Load()
    {
        Instance = this;

        // Initialize the config system
        ConfigManager = new ConfigManager();
        ConfigManager.Initialize(Directory);

        // Load the config (creates file with defaults if it does not exist)
        ConfigManager.LoadConfig<BountyConfig>();

        // Log config values
        var config = ConfigManager.GetConfig<BountyConfig>();
        Logger.Log($"Bounty range: {config.MinBounty} - {config.MaxBounty}");
        Logger.Log($"Tax rate: {config.TaxRate * 100}%");

        // Initialize the database
        Database = new BountyDatabase();

        // Auto-generate command documentation
        CommandDocGenerator.Generate(Directory);

        Logger.Log("BountyPlugin loaded!");
    }

    protected override void Unload()
    {
        Instance = null;
        Logger.Log("BountyPlugin unloaded.");
    }
}
```

## Command Usage Summary

Once the plugin is loaded, players and console can use:

```
/bounty set <player> <amount>    -- Place a bounty
/bounty list                     -- View all active bounties
/bounty admin reset <player>     -- Admin: reset a bounty
/bounty admin reload             -- Admin: reload config from disk
/b set PlayerName 500            -- Using the "b" alias
/bounty a reset PlayerName       -- Using the "a" alias for admin group
```

## Permissions

```xml
<Permission Cooldown="0">bounty</Permission>
<Permission Cooldown="0">bounty.set</Permission>
<Permission Cooldown="0">bounty.list</Permission>
<Permission Cooldown="0">bounty.admin</Permission>
<Permission Cooldown="0">bounty.admin.reset</Permission>
<Permission Cooldown="0">bounty.admin.reload</Permission>
```

## Key Patterns to Note

1. **Config with auto-migration**: If you add a new property to `BountyConfig` and redeploy, existing servers will automatically pick up the default value for the new property without losing their customized values for existing properties.

2. **Thread-safe messaging**: `MessageHelper.Say` can be called from any thread. It internally dispatches to the main thread via `ThreadHelper.RunSynchronously`.

3. **Background work pattern**: Use `ThreadHelper.RunAsynchronously` for database/HTTP calls, then use `MessageHelper.Say` to report results. No need to manually dispatch back to the main thread for messaging.

4. **SubCommandGroup for nesting**: The `admin` group is a `SubCommandGroup` that routes `reset` and `reload` as nested sub-commands. The group itself handles permission checking and argument routing.

5. **Console compatibility**: Using `AllowedCaller.Both` and `CommandBase.Reply` ensures commands work from both in-game chat and the server console. Console callers skip permission checks automatically.

6. **Command documentation**: Calling `CommandDocGenerator.Generate(Directory)` during plugin load produces a `{Directory}/Commands/bounty.md` file with a complete reference of all sub-commands, syntax, and permissions.

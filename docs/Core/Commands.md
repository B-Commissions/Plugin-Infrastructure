# Command Framework

BlueBeard.Core provides a structured command framework in the `BlueBeard.Core.Commands` namespace that builds on top of Rocket's `IRocketCommand`. It supports automatic sub-command routing, nested command groups, permission checking, and a unified reply mechanism that works for both in-game players and the server console.

## CommandBase

`CommandBase` is the abstract base class for all top-level commands. It implements `IRocketCommand` and adds automatic sub-command dispatch.

```csharp
namespace BlueBeard.Core.Commands;

public abstract class CommandBase : IRocketCommand
{
    public abstract AllowedCaller AllowedCaller { get; }
    public abstract string Name { get; }
    public abstract string Help { get; }
    public abstract string Syntax { get; }
    public abstract List<string> Aliases { get; }
    public abstract List<string> Permissions { get; }
    public abstract SubCommand[] Children { get; }
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `AllowedCaller` | `AllowedCaller` | Who can execute this command. Use `AllowedCaller.Both` for commands that work from console and in-game. |
| `Name` | `string` | The primary command name (e.g., `"bounty"`). |
| `Help` | `string` | A short description of what the command does. |
| `Syntax` | `string` | Usage syntax string. |
| `Aliases` | `List<string>` | Alternative names for the command. |
| `Permissions` | `List<string>` | Rocket permission strings for the top-level command. |
| `Children` | `SubCommand[]` | Array of sub-commands this command routes to. |

### Auto-Routing

When `Execute` is called, `CommandBase` performs the following:

1. If no arguments are provided, it displays a usage message listing all child sub-command names.
2. Otherwise, it takes the first argument as a token and finds the first child whose `Name` or `Aliases` match (case-insensitive).
3. If no matching child is found, it displays an error with the available sub-commands.
4. If the caller is an `UnturnedPlayer`, it checks `player.HasPermission(child.Permission)`. If the check fails, a permission-denied message is sent.
5. If the caller is the console, permission checking is skipped entirely.
6. The matched child's `Execute` method is called with the remaining arguments (everything after the matched token).

The router is `async void`, so sub-commands can perform asynchronous work. Any unhandled exceptions are caught, logged, and a generic error message is sent to the caller.

### Reply Helper

```csharp
public static void Reply(IRocketPlayer caller, string message, Color color = default)
```

A static helper that sends a message to either a player or the console:

- If the caller is an `UnturnedPlayer`, it calls `UnturnedChat.Say(player, message, color)`.
- If the caller is the console, it calls `Logger.Log(message)` (color is ignored).
- If `color` is not specified, it defaults to `Color.white`.

This method can be called from any `SubCommand` via `CommandBase.Reply(...)`.

## SubCommand

`SubCommand` is the abstract base class for individual sub-commands.

```csharp
namespace BlueBeard.Core.Commands;

public abstract class SubCommand
{
    public abstract string Name { get; }
    public virtual string[] Aliases => [];
    public abstract string Permission { get; }
    public abstract string Help { get; }
    public abstract string Syntax { get; }

    public bool Matches(string token);
    public abstract Task Execute(IRocketPlayer caller, string[] args);
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | The primary name of this sub-command. |
| `Aliases` | `string[]` | Alternative names. Defaults to an empty array. Override to provide aliases. |
| `Permission` | `string` | The Rocket permission string required to use this sub-command. |
| `Help` | `string` | A short description. |
| `Syntax` | `string` | Usage syntax for this sub-command's arguments. |

### Matches Method

```csharp
public bool Matches(string token)
```

Returns `true` if the given `token` equals `Name` or any entry in `Aliases` (case-insensitive comparison using `StringComparison.OrdinalIgnoreCase`).

### Execute Method

```csharp
public abstract Task Execute(IRocketPlayer caller, string[] args);
```

The `args` array contains only the arguments that follow this sub-command's token. For example, if the user types `/bounty set 500 player`, and this sub-command matched `set`, then `args` is `["500", "player"]`.

The method returns a `Task`, so it supports `async/await` for database calls, HTTP requests, or other asynchronous operations.

## SubCommandGroup

`SubCommandGroup` is a concrete implementation of `SubCommand` that acts as a routing node for nested sub-command trees. It does not contain any business logic itself; instead it routes to its own children using the same pattern as `CommandBase`.

```csharp
namespace BlueBeard.Core.Commands;

public class SubCommandGroup : SubCommand
{
    public SubCommandGroup(
        string name,
        string[] aliases,
        string permission,
        SubCommand[] children
    );
}
```

### Constructor Parameters

| Parameter | Description |
|-----------|-------------|
| `name` | The group's name (used for matching). |
| `aliases` | Alternative names for this group. |
| `permission` | The permission required to access this group. |
| `children` | The sub-commands within this group. |

### Behavior

- `Help` auto-generates as `"Manage {name}s"`.
- `Syntax` auto-generates as a pipe-separated list of child names.
- `Execute` performs the same routing logic as `CommandBase`: match first arg to a child, check permissions for `UnturnedPlayer`, delegate to the matched child with the remaining args.

## Full Example

Here is a complete command tree for a `/zone` command with nested sub-command groups:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueBeard.Core.Commands;
using Rocket.API;
using UnityEngine;

// -- Top-level command --

public class ZoneCommand : CommandBase
{
    public override AllowedCaller AllowedCaller => AllowedCaller.Both;
    public override string Name => "zone";
    public override string Help => "Manage zones";
    public override string Syntax => "<create | delete | flag>";
    public override List<string> Aliases => new() { "z" };
    public override List<string> Permissions => new() { "zone" };

    public override SubCommand[] Children => new SubCommand[]
    {
        new ZoneCreateCommand(),
        new ZoneDeleteCommand(),
        new SubCommandGroup("flag", new[] { "f" }, "zone.flag", new SubCommand[]
        {
            new FlagAddCommand(),
            new FlagRemoveCommand(),
            new FlagListCommand(),
        }),
    };
}

// -- Direct sub-commands --

public class ZoneCreateCommand : SubCommand
{
    public override string Name => "create";
    public override string[] Aliases => new[] { "c", "new" };
    public override string Permission => "zone.create";
    public override string Help => "Create a new zone";
    public override string Syntax => "<name>";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, "Usage: /zone create <name>", Color.yellow);
            return Task.CompletedTask;
        }

        var zoneName = args[0];
        // ... create zone logic ...
        CommandBase.Reply(caller, $"Zone '{zoneName}' created.", Color.green);
        return Task.CompletedTask;
    }
}

public class ZoneDeleteCommand : SubCommand
{
    public override string Name => "delete";
    public override string[] Aliases => new[] { "d", "remove" };
    public override string Permission => "zone.delete";
    public override string Help => "Delete an existing zone";
    public override string Syntax => "<name>";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, "Usage: /zone delete <name>", Color.yellow);
            return Task.CompletedTask;
        }

        var zoneName = args[0];
        // ... delete zone logic ...
        CommandBase.Reply(caller, $"Zone '{zoneName}' deleted.", Color.red);
        return Task.CompletedTask;
    }
}

// -- Nested sub-commands inside the "flag" group --

public class FlagAddCommand : SubCommand
{
    public override string Name => "add";
    public override string Permission => "zone.flag.add";
    public override string Help => "Add a flag to a zone";
    public override string Syntax => "<zone> <flag>";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 2)
        {
            CommandBase.Reply(caller, "Usage: /zone flag add <zone> <flag>", Color.yellow);
            return Task.CompletedTask;
        }

        // ... add flag logic ...
        CommandBase.Reply(caller, $"Flag '{args[1]}' added to zone '{args[0]}'.", Color.green);
        return Task.CompletedTask;
    }
}

public class FlagRemoveCommand : SubCommand
{
    public override string Name => "remove";
    public override string[] Aliases => new[] { "rm" };
    public override string Permission => "zone.flag.remove";
    public override string Help => "Remove a flag from a zone";
    public override string Syntax => "<zone> <flag>";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 2)
        {
            CommandBase.Reply(caller, "Usage: /zone flag remove <zone> <flag>", Color.yellow);
            return Task.CompletedTask;
        }

        // ... remove flag logic ...
        CommandBase.Reply(caller, $"Flag '{args[1]}' removed from zone '{args[0]}'.", Color.red);
        return Task.CompletedTask;
    }
}

public class FlagListCommand : SubCommand
{
    public override string Name => "list";
    public override string[] Aliases => new[] { "ls" };
    public override string Permission => "zone.flag.list";
    public override string Help => "List all flags on a zone";
    public override string Syntax => "<zone>";

    public override Task Execute(IRocketPlayer caller, string[] args)
    {
        if (args.Length < 1)
        {
            CommandBase.Reply(caller, "Usage: /zone flag list <zone>", Color.yellow);
            return Task.CompletedTask;
        }

        // ... list flags logic ...
        CommandBase.Reply(caller, $"Flags for '{args[0]}': pvp, no-build", Color.cyan);
        return Task.CompletedTask;
    }
}
```

### Usage from in-game or console

```
/zone create Safezone
/zone delete Safezone
/zone flag add Safezone pvp
/zone flag remove Safezone pvp
/zone flag list Safezone
/zone f add Safezone pvp       (using the "f" alias for the flag group)
/z create Safezone             (using the "z" alias for the top-level command)
```

### Permission Tree

```
zone              - Access the /zone command
zone.create       - Create zones
zone.delete       - Delete zones
zone.flag         - Access the flag sub-group
zone.flag.add     - Add flags
zone.flag.remove  - Remove flags
zone.flag.list    - List flags
```

Note: Console callers bypass all permission checks. Permission checking only applies to `UnturnedPlayer` instances.

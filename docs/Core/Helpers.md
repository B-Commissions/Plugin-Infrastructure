# Helpers Reference

All helpers are in the `BlueBeard.Core.Helpers` namespace. They provide commonly needed utilities for threading, messaging, raycasting, barricade manipulation, and documentation generation.

## ThreadHelper

Bridges background threads and Unity's main thread. Unturned (Unity) requires most game API calls to happen on the main thread, while long-running or blocking operations (database, HTTP) should happen on background threads. `ThreadHelper` makes this easy.

### API

```csharp
public class ThreadHelper
{
    public static void RunAsynchronously(Action action, string exceptionMessage = null);
    public static void RunAsynchronously(Func<Task> asyncAction, string exceptionMessage = null);
    public static void RunSynchronously(Action action, float delaySeconds = 0);
}
```

### Methods

| Method | Description |
|--------|-------------|
| `RunAsynchronously(Action, string)` | Queues the action on `ThreadPool`. Exceptions are caught and logged on the main thread via `RunSynchronously`. |
| `RunAsynchronously(Func<Task>, string)` | Runs the async delegate via `Task.Run`. Exceptions are caught and logged on the main thread. |
| `RunSynchronously(Action, float)` | Dispatches the action to Unity's main thread using `TaskDispatcher.QueueOnMainThread`. Optionally delays by `delaySeconds`. |

### Examples

```csharp
using BlueBeard.Core.Helpers;

// Run a database query on a background thread
ThreadHelper.RunAsynchronously(() =>
{
    var result = database.QueryPlayerStats(steamId);

    // Jump back to the main thread to update game state
    ThreadHelper.RunSynchronously(() =>
    {
        player.Hunger = result.Hunger;
    });
}, "Failed to load player stats");

// Run an async HTTP request on a background thread
ThreadHelper.RunAsynchronously(async () =>
{
    var response = await httpClient.GetAsync("https://api.example.com/data");
    var json = await response.Content.ReadAsStringAsync();

    ThreadHelper.RunSynchronously(() =>
    {
        ProcessApiResponse(json);
    });
}, "API request failed");

// Delay an action on the main thread by 2 seconds
ThreadHelper.RunSynchronously(() =>
{
    UnturnedChat.Say("The event starts now!");
}, 2f);
```

## MessageHelper

Thread-safe player and server messaging. Internally uses `ThreadHelper.RunSynchronously` to dispatch `UnturnedChat.Say` calls to the main thread, so it is safe to call from any thread.

### API

```csharp
public class MessageHelper
{
    public static void Say(IRocketPlayer caller, string message, Color color = default);
    public static void Say(string message, Color color = default);
}
```

### Methods

| Method | Description |
|--------|-------------|
| `Say(IRocketPlayer, string, Color)` | Sends a message to a specific player. Dispatches to the main thread automatically. |
| `Say(string, Color)` | Broadcasts a message to all players on the server. Dispatches to the main thread automatically. |

Both methods default to `Color.white` if no color is specified.

### Examples

```csharp
using BlueBeard.Core.Helpers;
using UnityEngine;

// Safe to call from a background thread
ThreadHelper.RunAsynchronously(() =>
{
    var balance = database.GetBalance(player.CSteamID);

    // This is safe even though we are on a background thread
    MessageHelper.Say(player, $"Your balance: ${balance}", Color.green);
});

// Broadcast to all players
MessageHelper.Say("Server restart in 5 minutes!", Color.yellow);
```

## SurfaceHelper

Raycasting utility that snaps a position down to the nearest surface. Useful for placing objects or teleporting players to the ground.

### API

```csharp
public class SurfaceHelper
{
    public static Vector3 SnapPositionToSurface(Vector3 position, int? layerMask = null);
}
```

### Behavior

1. Creates a ray origin at `(position.x, 1024, position.z)`.
2. Raycasts straight down with a max distance of 2048.
3. If a hit is found, returns `hit.point`.
4. If no hit is found, returns the original `position` unchanged.

### Default Layer Mask

The default layer mask includes:
- `RayMasks.GROUND`
- `RayMasks.BARRICADE`
- `RayMasks.STRUCTURE`
- `RayMasks.ENVIRONMENT`

You can override this by passing a custom `layerMask` parameter.

### Examples

```csharp
using BlueBeard.Core.Helpers;
using UnityEngine;

// Snap a position to the ground using the default mask
var groundPos = SurfaceHelper.SnapPositionToSurface(new Vector3(100, 500, 200));

// Snap using only the GROUND layer
var groundOnly = SurfaceHelper.SnapPositionToSurface(
    new Vector3(100, 500, 200),
    RayMasks.GROUND
);

// Teleport a player to a snapped position
var targetPos = SurfaceHelper.SnapPositionToSurface(somePosition);
player.Player.teleportToLocation(targetPos, player.Player.look.yaw);
```

## BarricadeHelper

Utilities for looking up barricade information from raycast hits and changing barricade ownership.

### BarricadeInfo Struct

```csharp
public struct BarricadeInfo
{
    public Transform Transform;    // The barricade's Transform
    public Vector3 Position;       // World position
    public Vector3 Rotation;       // Euler angles rotation
    public string AssetName;       // The asset name (e.g., "Metal Door")
    public ushort AssetId;         // The asset ID
    public string State;           // Base64-encoded barricade state
}
```

### API

```csharp
public static class BarricadeHelper
{
    public static bool TryGetBarricadeFromHit(Transform hitTransform, out BarricadeInfo info);
    public static void ChangeBarricadeOwner(Transform transform, CSteamID steamID, CSteamID groupID);
}
```

### TryGetBarricadeFromHit

Walks up the transform hierarchy from the given `hitTransform` to find a barricade registered with `BarricadeManager`. Returns `true` and populates `info` if a barricade is found, or `false` with a default `info` if not.

This is useful when processing raycast results from player interactions:

```csharp
using BlueBeard.Core.Helpers;

// In a raycast callback
if (BarricadeHelper.TryGetBarricadeFromHit(hit.transform, out var info))
{
    CommandBase.Reply(caller, $"Barricade: {info.AssetName} (ID: {info.AssetId})", Color.cyan);
    CommandBase.Reply(caller, $"Position: {info.Position}", Color.cyan);
    CommandBase.Reply(caller, $"State: {info.State}", Color.cyan);
}
else
{
    CommandBase.Reply(caller, "No barricade found at that location.", Color.red);
}
```

### ChangeBarricadeOwner

Changes the owner and group of a barricade. Updates both the replicated state (so the change is visible to clients) and the internal ownership records.

```csharp
using BlueBeard.Core.Helpers;
using Steamworks;

// Transfer ownership of a barricade to a different player
var newOwner = new CSteamID(76561198012345678);
var newGroup = new CSteamID(0); // No group
BarricadeHelper.ChangeBarricadeOwner(barricadeTransform, newOwner, newGroup);
```

The method:
1. Constructs a 17-byte state array containing the new owner Steam ID (bytes 0-7), group Steam ID (bytes 8-15), and a boolean flag (byte 16).
2. Calls `BarricadeManager.updateReplicatedState` to sync the state to clients.
3. Calls `BarricadeManager.changeOwnerAndGroup` to update the server-side ownership records.

## CommandDocGenerator

Auto-generates markdown documentation for all `CommandBase` subclasses in the calling assembly. This is useful for producing player-facing command references automatically.

### API

```csharp
public static class CommandDocGenerator
{
    public static void Generate(string pluginDirectory);
}
```

### Behavior

1. Creates a `Commands/` directory inside `pluginDirectory` if it does not exist.
2. Scans the **calling assembly** for all non-abstract subclasses of `CommandBase`.
3. For each command, creates an instance and generates a markdown file named `{commandName}.md`.
4. The generated markdown includes:
   - The command name as a heading
   - Help text, syntax, permissions, and aliases
   - A "Subcommands" section with tables for direct sub-commands
   - Nested headings for `SubCommandGroup` entries, with their own child tables

### Generated Output Format

For each command, the generated file looks like:

```markdown
# /zone

Manage zones

**Syntax:** `<create | delete | flag>`
**Permissions:** `zone`
**Aliases:** `z`

## Subcommands

| Command | Syntax | Permission | Description |
|---------|--------|------------|-------------|
| `create` | `<name>` | `zone.create` | Create a new zone |
| `delete` | `<name>` | `zone.delete` | Delete an existing zone |

### flag

**Permission:** `zone.flag`

| Command | Syntax | Permission | Description |
|---------|--------|------------|-------------|
| `add` | `<zone> <flag>` | `zone.flag.add` | Add a flag to a zone |
| `remove` | `<zone> <flag>` | `zone.flag.remove` | Remove a flag from a zone |
| `list` | `<zone>` | `zone.flag.list` | List all flags on a zone |
```

### Example Usage

```csharp
using BlueBeard.Core.Helpers;
using Rocket.Core.Plugins;

public class MyPlugin : RocketPlugin
{
    protected override void Load()
    {
        // Generate docs for all commands in this plugin's assembly
        CommandDocGenerator.Generate(Directory);

        // Files are written to {Directory}/Commands/*.md
    }
}
```

Note: `CommandDocGenerator` uses `Assembly.GetCallingAssembly()`, so it must be called directly from the plugin assembly whose commands you want to document. It will not pick up commands from other assemblies.

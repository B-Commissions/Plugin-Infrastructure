# RocketMod Adapter

`BlueBeard.RocketMod` implements the [BlueBeard.Core abstractions](Abstractions.md) against RocketMod's APIs.

## Reference

```xml
<ProjectReference Include="..\BlueBeard.RocketMod\BlueBeard.RocketMod.csproj" />
```

The adapter pulls in `Rocket.API`, `Rocket.Core`, `Rocket.Unturned` (via `Libs/`), plus `BlueBeard.Core`. No NuGet packages are added.

## Bootstrap

```csharp
public class MyPlugin : RocketPlugin
{
    protected override void Load() => RocketModBootstrap.Install();
    protected override void Unload() => RocketModBootstrap.Uninstall();
}
```

`Install()` configures all five `BlueBeardHost` services in one call. `Uninstall()` clears them and detaches the player-events subscription.

## What each adapter does

| Service | Class | Maps to |
|---------|-------|---------|
| `ILogger` | `RocketLogger` | `Rocket.Core.Logging.Logger.Log` / `LogWarning` / `LogError` / `LogException` |
| `IChat` | `RocketChat` | `UnturnedChat.Say(player, message, color)` and broadcast variants |
| `IPermissions` | `RocketPermissions` | `IRocketPlayer.HasPermission` (extension) and `R.Permissions.AddPlayerToGroup` / `RemovePlayerFromGroup` |
| `ITaskDispatcher` | `RocketTaskDispatcher` | `Rocket.Core.Utils.TaskDispatcher.QueueOnMainThread` |
| `IPlayerEvents` | `RocketPlayerEvents` | `U.Events.OnPlayerConnected` / `OnPlayerDisconnected` |
| `IPlayer` | `RocketPlayer` | Wraps `UnturnedPlayer` / SDG `Player` |

## Wrapping a Rocket player

When a BlueBeard library hands you back an `IPlayer` (or you need to convert), use the `RocketPlayer` factories:

```csharp
using BlueBeard.RocketMod;

IPlayer wrapped = RocketPlayer.From(unturnedPlayer);  // from Rocket's UnturnedPlayer
IPlayer wrapped = RocketPlayer.From(sdgPlayer);       // from SDG.Unturned.Player
IPlayer console = RocketPlayer.Console;               // shared console actor
```

The underlying SDG `Player` is reachable via `wrapped.Unturned` (null for console).

## Permission defaults

RocketMod's `HasPermission` returns true for unset nodes (permissive default). The adapter preserves this — there's no flip in either direction. If you also need your code to behave the same way under OpenMod, register the relevant permission nodes explicitly there.

## Threading

`ITaskDispatcher.QueueOnMainThread` is a thin pass-through to `TaskDispatcher.QueueOnMainThread` — same behaviour, same delay semantics.

## Reload safety

`RocketModBootstrap.Install()` is idempotent: calling it twice replaces the previous service instances and re-uses the same `RocketPlayerEvents` instance (which keeps a single subscription on `U.Events`).

## Using alongside existing direct Rocket calls

The adapter is additive. Existing libraries that still call `Rocket.Core.Logging.Logger.Log` or `UnturnedChat.Say` continue to work — they bypass `BlueBeardHost` entirely. Migration to the abstraction is per-library and incremental.

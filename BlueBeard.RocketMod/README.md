# BlueBeard.RocketMod

RocketMod host adapter for [BlueBeard.Core](../BlueBeard.Core/README.md) abstractions. Implements `ILogger`, `IChat`, `IPermissions`, `ITaskDispatcher`, `IPlayer`, and `IPlayerEvents` against RocketMod / Unturned's APIs.

## Why this exists

`BlueBeard.Core/Abstractions` defines framework-agnostic contracts so the same library can run under either RocketMod or OpenMod. `BlueBeard.RocketMod` is the RocketMod implementation of those contracts.

## Usage

In your `RocketPlugin.Load()`:

```csharp
using BlueBeard.RocketMod;
using BlueBeard.Core.Abstractions;

protected override void Load()
{
    RocketModBootstrap.Install();

    // Now any BlueBeard library that uses BlueBeardHost services will route through
    // the Rocket adapter:
    BlueBeardHost.Logger.Log("Hello from BlueBeard");
    BlueBeardHost.Chat.Broadcast("Server is up", Color.green);
}

protected override void Unload()
{
    RocketModBootstrap.Uninstall();
}
```

## Wrapping a Rocket player

Convert a RocketMod player to the abstraction with `RocketPlayer.From(unturnedPlayer)`. Use `RocketPlayer.Console` for the server console actor.

## Migration from direct Rocket calls

This package is additive — existing libraries that call `Rocket.Core.Logging.Logger.Log` or `UnturnedChat.Say` directly continue to work. The migration to `BlueBeardHost.Logger` / `BlueBeardHost.Chat` is per-library and can be done incrementally.

# Mod-Host Adapters

BlueBeard libraries can run under either **RocketMod** or **OpenMod**. The two are wire-compatible: the same `BlueBeard.Core`, `BlueBeard.Zones`, `BlueBeard.Items`, etc. binaries are loaded, but a thin host-specific adapter installs the implementations of `ILogger`, `IChat`, `IPermissions`, `ITaskDispatcher`, and `IPlayerEvents` against the chosen framework's APIs.

## How it fits together

```
+-------------------------------------------+
| BlueBeard.Zones, BlueBeard.Items, ...     |
| (call BlueBeardHost.Logger / Chat / ...)  |
+-------------------------------------------+
              |
              v
+-------------------------------------------+
| BlueBeard.Core/Abstractions               |
| ILogger, IChat, IPermissions,             |
| ITaskDispatcher, IPlayerEvents,           |
| IPlayer, ICommand, IPluginHost,           |
| BlueBeardHost (static service locator)    |
+-------------------------------------------+
              ^                       ^
              |                       |
+----------------------+    +---------------------+
| BlueBeard.RocketMod  |    | BlueBeard.OpenMod   |
| (Rocket adapters)    |    | (OpenMod adapters)  |
+----------------------+    +---------------------+
```

`BlueBeard.Core` itself does not reference RocketMod or OpenMod. Each adapter project does, and your plugin chooses which one to depend on.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Pick an adapter, install it, verify it works |
| [Abstractions](Abstractions.md) | The interfaces in `BlueBeard.Core/Abstractions` |
| [RocketMod Adapter](RocketMod-Adapter.md) | `BlueBeard.RocketMod` setup and behaviour |
| [OpenMod Adapter](OpenMod-Adapter.md) | `BlueBeard.OpenMod` setup and behaviour |

## Why the indirection

Direct calls to `Rocket.Core.Logging.Logger.Log(...)` or `UnturnedChat.Say(...)` couple a library to RocketMod and prevent it from running under OpenMod. The abstraction layer is the seam that lets one library binary serve both hosts.

## Status

- **Adapters shipped**: `BlueBeard.RocketMod`, `BlueBeard.OpenMod`. Both build clean against the abstractions.
- **Per-library migration**: existing libraries (`Zones`, `Items`, etc.) still call Rocket APIs directly today. Migration to `BlueBeardHost.*` is incremental — see each library's docs for migration status. New library code should always use the abstraction.

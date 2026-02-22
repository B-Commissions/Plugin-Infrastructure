# BlueBeard.Core

BlueBeard.Core is the foundation library for all BlueBeard Unturned plugins. Every plugin in the BlueBeard ecosystem depends on this library for its core abstractions, configuration management, command framework, and utility helpers.

## What It Provides

### IManager Lifecycle Interface

The `IManager` interface (namespace `BlueBeard.Core`) defines a standard lifecycle contract for plugin subsystems:

```csharp
public interface IManager
{
    void Load();
    void Unload();
}
```

Any component that needs startup/shutdown behavior should implement `IManager`.

### ConfigManager - XML-Based Configuration System

`ConfigManager` (namespace `BlueBeard.Core.Configs`) provides a robust XML configuration system with:

- Type-safe config loading and retrieval via generics
- Automatic file creation with defaults for first-run scenarios
- Auto-migration: new properties receive defaults, removed properties are cleaned up, null reference-type properties are repaired
- Configs stored as `{TypeName}.configuration.xml` inside a `Configs/` subfolder

### IConfig Interface

The `IConfig` interface (namespace `BlueBeard.Core.Configs`) is the contract all configuration classes must implement:

```csharp
public interface IConfig
{
    void LoadDefaults();
}
```

### Command Framework

The command framework (namespace `BlueBeard.Core.Commands`) provides a structured way to build Rocket commands with automatic sub-command routing:

- **`CommandBase`** - Abstract base class extending `IRocketCommand` with built-in sub-command dispatch
- **`SubCommand`** - Abstract class for individual sub-commands with name/alias matching
- **`SubCommandGroup`** - Concrete class for grouping sub-commands into nested trees

### Helpers

The helpers (namespace `BlueBeard.Core.Helpers`) provide commonly needed utilities:

- **`ThreadHelper`** - Bridge between background threads and the Unity main thread
- **`MessageHelper`** - Thread-safe player/server messaging
- **`SurfaceHelper`** - Raycasting utility to snap positions to the ground surface
- **`BarricadeHelper`** - Barricade lookup and ownership modification
- **`CommandDocGenerator`** - Auto-generates markdown documentation for all commands in a plugin

## Documentation Pages

| Page | Description |
|------|-------------|
| [Configuration](Configuration.md) | Config system deep dive: ConfigManager, IConfig, auto-migration, and examples |
| [Commands](Commands.md) | Command framework guide: CommandBase, SubCommand, SubCommandGroup, routing, and permissions |
| [Helpers](Helpers.md) | Complete helpers reference: ThreadHelper, MessageHelper, SurfaceHelper, BarricadeHelper, CommandDocGenerator |
| [Examples](Examples.md) | Full implementation examples: a complete plugin with config, commands, async work, and messaging |

## Namespaces at a Glance

| Namespace | Contents |
|-----------|----------|
| `BlueBeard.Core` | `IManager` |
| `BlueBeard.Core.Configs` | `ConfigManager`, `IConfig` |
| `BlueBeard.Core.Commands` | `CommandBase`, `SubCommand`, `SubCommandGroup` |
| `BlueBeard.Core.Helpers` | `ThreadHelper`, `MessageHelper`, `SurfaceHelper`, `BarricadeHelper`, `CommandDocGenerator` |

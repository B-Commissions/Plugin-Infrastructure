# BlueBeard Infrastructure

A collection of shared libraries for building Unturned RocketMod plugins. Each library solves a specific domain problem -- configuration, persistence, effects, UI, zones -- so plugin authors can focus on gameplay logic instead of re-implementing boilerplate.

## Why This Exists

Every Unturned plugin needs the same foundational pieces: a config system, a way to talk to MySQL, thread-safe messaging, spatial triggers, UI management. Rather than duplicating this code across plugins, the BlueBeard Infrastructure extracts these concerns into focused, reusable libraries that any RocketMod plugin can reference.

## Libraries

| Library | Purpose |
|---------|---------|
| [BlueBeard.Core](docs/Core/) | Foundation: config management, IManager lifecycle, thread helpers, chat messaging, barricade utilities, command framework |
| [BlueBeard.Database](docs/Database/) | Lightweight MySQL ORM with attribute-based entities, LINQ-to-SQL expressions, and automatic schema sync |
| [BlueBeard.Effects](docs/Effects/) | Managed effect emitter system with spatial patterns (circle, scatter, square) and audience targeting |
| [BlueBeard.Holograms](docs/Holograms/) | Proximity-based 3D holograms with pooled UI overlays, per-player state, and dynamic metadata |
| [BlueBeard.UI](docs/UI/) | Full-screen UI framework with hierarchical screens/dialogs, automatic event routing, and per-player state |
| [BlueBeard.Zones](docs/Zones/) | Advanced zone management with trigger colliders, persistent storage, 26 enforcement flags, block lists, and CLI administration |

## Dependency Graph

```
BlueBeard.Core
  |
  +-- BlueBeard.Database -----> MySqlConnector
  |
  +-- BlueBeard.Effects
  |
  +-- BlueBeard.Holograms
  |
  +-- BlueBeard.UI
  |
  +-- BlueBeard.Zones ----------> BlueBeard.Database
                                  Newtonsoft.Json
```

All libraries depend on **BlueBeard.Core**. Only **BlueBeard.Zones** has a second internal dependency on **BlueBeard.Database** (for MySQL storage). External dependencies are minimal: **MySqlConnector** for database access and **Newtonsoft.Json** for JSON serialization.

## Requirements

- .NET Framework 4.8.1
- Unturned Dedicated Server (Assembly-CSharp, UnityEngine)
- RocketMod (Rocket.API, Rocket.Core, Rocket.Unturned)
- Steamworks.NET, SDG.NetTransport

All Unturned and RocketMod assemblies are expected in the `Libs/` folder.

## Quick Start

### Referencing a Library

Add a project reference in your plugin's `.csproj`:

```xml
<ProjectReference Include="..\BlueBeard.Core\BlueBeard.Core.csproj" />
```

### Minimal Plugin Using Core

```csharp
using BlueBeard.Core.Configs;
using Rocket.Core.Plugins;

public class MyPlugin : RocketPlugin
{
    private ConfigManager _configManager;

    protected override void Load()
    {
        _configManager = new ConfigManager();
        _configManager.Initialize(Directory);
        _configManager.LoadConfig<MyConfig>();

        var config = _configManager.GetConfig<MyConfig>();
        Logger.Log($"Loaded with max players: {config.MaxPlayers}");
    }
}
```

### Using Multiple Libraries Together

```csharp
using BlueBeard.Core.Configs;
using BlueBeard.Database;
using BlueBeard.Effects;
using BlueBeard.UI;

public class MyPlugin : RocketPlugin
{
    public ConfigManager ConfigManager { get; private set; }
    public DatabaseManager Database { get; private set; }
    public EffectEmitterManager Effects { get; private set; }
    public UIManager UI { get; private set; }

    protected override void Load()
    {
        ConfigManager = new ConfigManager();
        ConfigManager.Initialize(Directory);
        ConfigManager.LoadConfig<MyConfig>();
        ConfigManager.LoadConfig<DatabaseConfig>();

        Database = new DatabaseManager();
        Database.Initialize(ConfigManager);
        Database.RegisterEntity<PlayerData>();
        Database.Load();

        Effects = new EffectEmitterManager();
        Effects.Load();

        UI = new UIManager();
        UI.Load();
    }

    protected override void Unload()
    {
        UI.Unload();
        Effects.Unload();
        Database.Unload();
    }
}
```

## Documentation

Full documentation for each library is in the [docs/](docs/) folder, organized by project:

- [Core](docs/Core/) -- Config system, helpers, command framework
- [Database](docs/Database/) -- Entity definitions, queries, schema sync
- [Effects](docs/Effects/) -- Patterns, audiences, emitter lifecycle
- [Holograms](docs/Holograms/) -- Pools, displays, proximity triggers
- [UI](docs/UI/) -- Screens, dialogs, event routing, per-player state
- [Zones](docs/Zones/) -- Zone shapes, flags, storage, player tracking, commands

## Building

```bash
dotnet build Infastructure.sln
```

## Project Structure

```
Infastructure/
  BlueBeard.Core/          Foundation library
  BlueBeard.Database/      MySQL ORM
  BlueBeard.Effects/       Effect emitter system
  BlueBeard.Holograms/     Proximity hologram system
  BlueBeard.UI/            Full-screen UI framework
  BlueBeard.Zones/         Zone management system
  Libs/                    Unturned + RocketMod assemblies
  docs/                    Per-project documentation
  Infastructure.sln        Solution file
```

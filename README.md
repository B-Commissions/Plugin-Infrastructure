# BlueBeard Infrastructure

A collection of shared libraries for building Unturned plugins. Each library solves a specific domain problem -- configuration, persistence, effects, UI, zones -- so plugin authors can focus on gameplay logic instead of re-implementing boilerplate.

## Why This Exists

Every Unturned plugin needs the same foundational pieces: a config system, a way to talk to MySQL, thread-safe messaging, spatial triggers, UI management, item state encoding. Rather than duplicating this code across plugins, the BlueBeard Infrastructure extracts these concerns into focused, reusable libraries.

The libraries are **mod-host agnostic**: a thin adapter assembly per host (RocketMod or OpenMod) implements the framework-specific bits, so the same `BlueBeard.Core`, `BlueBeard.Items`, `BlueBeard.Zones`, etc. binaries run under either framework.

## Libraries

### Mod-host adapters

| Library | Purpose |
|---------|---------|
| [BlueBeard.Core/Abstractions](docs/Mods/Abstractions.md) | Framework-free interfaces for logging, chat, permissions, dispatch, player events, players, commands |
| [BlueBeard.RocketMod](docs/Mods/RocketMod-Adapter.md) | RocketMod adapter — implements the abstractions against RocketMod APIs |
| [BlueBeard.OpenMod](docs/Mods/OpenMod-Adapter.md) | OpenMod adapter — implements the abstractions against OpenMod APIs |

### Domain libraries

| Library | Purpose |
|---------|---------|
| [BlueBeard.Core](docs/Core/) | Foundation: config management, `IManager` lifecycle, thread helpers, chat messaging, barricade utilities, command framework, abstractions |
| [BlueBeard.Database](docs/Database/) | Lightweight MySQL ORM with attribute-based entities, LINQ-to-SQL expressions, and automatic schema sync |
| [BlueBeard.Effects](docs/Effects/) | Managed effect emitter system with spatial patterns (circle, scatter, square) and audience targeting |
| [BlueBeard.Holograms](docs/Holograms/) | Proximity-based 3D holograms with pooled UI overlays, per-player state, and dynamic metadata |
| [BlueBeard.Items](docs/Items/) | State encoding (`StateWriter` cursor + `ItemStateEncoder` static) and per-asset behaviour registries for items, barricades, structures, vehicles, zombies, and animals |
| [BlueBeard.UI](docs/UI/) | Full-screen UI framework with hierarchical screens/dialogs, automatic event routing, and per-player state |
| [BlueBeard.Zones](docs/Zones/) | Advanced zone management with trigger colliders, persistent storage, 26 enforcement flags, block lists, and CLI administration |

## Dependency Graph

```
BlueBeard.Core (incl. Abstractions)
  |
  +-- BlueBeard.Database -----> MySqlConnector
  |
  +-- BlueBeard.Effects
  |
  +-- BlueBeard.Holograms
  |
  +-- BlueBeard.Items
  |
  +-- BlueBeard.UI
  |
  +-- BlueBeard.Zones ----------> BlueBeard.Database
  |                              Newtonsoft.Json
  |
  +-- BlueBeard.RocketMod ------> Rocket.API/Core/Unturned
  |
  +-- BlueBeard.OpenMod --------> OpenMod.Unturned (NuGet)
                                  Microsoft.Extensions.Logging/DI
```

All domain libraries depend on **BlueBeard.Core**. **BlueBeard.RocketMod** and **BlueBeard.OpenMod** are alternatives — your plugin references one, not both.

## Requirements

- .NET Framework 4.8.1
- Unturned Dedicated Server (Assembly-CSharp, UnityEngine)
- One of:
  - **RocketMod** (Rocket.API, Rocket.Core, Rocket.Unturned) for plugins that reference `BlueBeard.RocketMod`
  - **OpenMod** for plugins that reference `BlueBeard.OpenMod`
- Steamworks.NET, SDG.NetTransport

Unturned and RocketMod assemblies are expected in the `Libs/` folder. OpenMod packages are pulled from NuGet.

## Quick Start

### Referencing libraries

In your plugin's `.csproj`:

```xml
<ProjectReference Include="..\BlueBeard.Core\BlueBeard.Core.csproj" />
<ProjectReference Include="..\BlueBeard.Items\BlueBeard.Items.csproj" />
<!-- Pick ONE host adapter -->
<ProjectReference Include="..\BlueBeard.RocketMod\BlueBeard.RocketMod.csproj" />
<!-- OR -->
<ProjectReference Include="..\BlueBeard.OpenMod\BlueBeard.OpenMod.csproj" />
```

### Minimal RocketMod plugin

```csharp
using BlueBeard.Core.Abstractions;
using BlueBeard.Core.Configs;
using BlueBeard.RocketMod;
using Rocket.Core.Plugins;

public class MyPlugin : RocketPlugin
{
    private ConfigManager _configManager;

    protected override void Load()
    {
        RocketModBootstrap.Install();           // installs BlueBeardHost services

        _configManager = new ConfigManager();
        _configManager.Initialize(Directory);
        _configManager.LoadConfig<MyConfig>();

        BlueBeardHost.Logger.Log($"Loaded with max players: {_configManager.GetConfig<MyConfig>().MaxPlayers}");
    }

    protected override void Unload()
    {
        RocketModBootstrap.Uninstall();
    }
}
```

### Minimal OpenMod plugin

See [docs/Mods/Getting-Started.md](docs/Mods/Getting-Started.md) for the OpenMod constructor injection wiring.

### Using multiple libraries together

```csharp
using BlueBeard.Core.Abstractions;
using BlueBeard.Core.Configs;
using BlueBeard.Database;
using BlueBeard.Effects;
using BlueBeard.Items;
using BlueBeard.Items.Behaviours;
using BlueBeard.RocketMod;
using BlueBeard.UI;

public class MyPlugin : RocketPlugin
{
    public ConfigManager ConfigManager { get; private set; }
    public DatabaseManager Database { get; private set; }
    public EffectEmitterManager Effects { get; private set; }
    public ItemBehaviourManager Items { get; private set; }
    public BarricadeBehaviourManager Barricades { get; private set; }
    public UIManager UI { get; private set; }

    protected override void Load()
    {
        RocketModBootstrap.Install();

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

        Items = new ItemBehaviourManager();
        Barricades = new BarricadeBehaviourManager();
        Items.Load();
        Barricades.Load();

        UI = new UIManager();
        UI.Load();
    }

    protected override void Unload()
    {
        UI.Unload();
        Barricades.Unload();
        Items.Unload();
        Effects.Unload();
        Database.Unload();
        RocketModBootstrap.Uninstall();
    }
}
```

## Documentation

Full documentation is in the [docs/](docs/) folder, organized by topic:

- [Mod-Host Adapters](docs/Mods/) -- Abstractions, RocketMod adapter, OpenMod adapter
- [Core](docs/Core/) -- Config system, helpers, command framework
- [Database](docs/Database/) -- Entity definitions, queries, schema sync
- [Effects](docs/Effects/) -- Patterns, audiences, emitter lifecycle
- [Holograms](docs/Holograms/) -- Pools, displays, proximity triggers
- [Items & Behaviours](docs/Items/) -- State encoding (cursor + static), behaviour registries for 6 entity types
- [UI](docs/UI/) -- Screens, dialogs, event routing, per-player state
- [Zones](docs/Zones/) -- Zone shapes, flags, storage, player tracking, commands

## Building

```bash
dotnet build Infastructure.sln
```

## Project Structure

```
Infastructure/
  BlueBeard.Core/          Foundation library (incl. Abstractions namespace)
  BlueBeard.RocketMod/     RocketMod adapter
  BlueBeard.OpenMod/       OpenMod adapter
  BlueBeard.Database/      MySQL ORM
  BlueBeard.Effects/       Effect emitter system
  BlueBeard.Holograms/     Proximity hologram system
  BlueBeard.Items/         State encoding + behaviour registries (items, barricades, structures, vehicles, zombies, animals)
  BlueBeard.UI/            Full-screen UI framework
  BlueBeard.Zones/         Zone management system
  BlueBeard.Tests/         xUnit tests
  Libs/                    Unturned + RocketMod assemblies
  docs/                    Per-project documentation
  Infastructure.sln        Solution file
```

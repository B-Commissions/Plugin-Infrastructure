# BlueBeard Infrastructure Documentation

Welcome to the BlueBeard Infrastructure documentation. Each library has its own section with usage guides, API references, and implementation examples.

## Libraries

### [Core](Core/Home.md)
Foundation library: configuration management, IManager lifecycle, command framework, and utility helpers.
- [Configuration](Core/Configuration.md) -- XML config system with auto-migration
- [Commands](Core/Commands.md) -- Command tree framework with routing and permissions
- [Helpers](Core/Helpers.md) -- ThreadHelper, MessageHelper, SurfaceHelper, BarricadeHelper
- [Examples](Core/Examples.md) -- Complete plugin example

### [Database](Database/Home.md)
Lightweight MySQL ORM with attribute-based entities and LINQ-to-SQL expressions.
- [Getting Started](Database/Getting-Started.md) -- Setup and initialization
- [Entities](Database/Entities.md) -- Defining entities with attributes
- [Queries](Database/Queries.md) -- CRUD operations and expression support
- [Examples](Database/Examples.md) -- Complete plugin with database operations

### [Effects](Effects/Home.md)
Managed effect emitter system with spatial patterns and audience targeting.
- [Getting Started](Effects/Getting-Started.md) -- Setup and emitter lifecycle
- [Patterns](Effects/Patterns.md) -- SinglePoint, Circle, Square, Scatter, custom
- [Audiences](Effects/Audiences.md) -- AllPlayers, SinglePlayer, PlayerGroup, custom
- [Examples](Effects/Examples.md) -- One-shot, repeating, filtered, and custom effects

### [Holograms](Holograms/Home.md)
Proximity-based 3D hologram system with pooled UI overlays.
- [Getting Started](Holograms/Getting-Started.md) -- Setup and core concepts
- [Pools and Allocation](Holograms/Pools-and-Allocation.md) -- Global vs per-player modes
- [Dynamic Updates](Holograms/Dynamic-Updates.md) -- Runtime updates and events
- [Examples](Holograms/Examples.md) -- Shop holograms, instanced displays, filtering

### [UI](UI/Home.md)
Full-screen UI framework with hierarchical screens, dialogs, and per-player state.
- [Getting Started](UI/Getting-Started.md) -- Setup and architecture
- [Interfaces](UI/Interfaces.md) -- IUI, IUIScreen, IUIDialog reference
- [Player State](UI/Player-State.md) -- UIContext and UIPlayerComponent
- [Event Routing](UI/Event-Routing.md) -- Button and text input dispatch
- [Examples](UI/Examples.md) -- Multi-screen UI with pagination and dialogs

### [Zones](Zones/Home.md)
Advanced zone management with triggers, persistent storage, flags, and administration.
- [Installation](Zones/Installation.md) -- Setup as plugin or library
- [Getting Started](Zones/Getting-Started.md) -- Creating your first zone
- [Commands](Zones/Commands.md) -- Full command reference
- [Flags](Zones/Flags.md) -- All 26 enforcement flags
- [Block Lists](Zones/Block-Lists.md) -- Item/build restrictions
- [Permissions](Zones/Permissions.md) -- Permission overrides
- [Configuration](Zones/Configuration.md) -- Plugin config options
- [Developer Quick Start](Zones/Developer-Quick-Start.md) -- Using Zones as a library
- [Zone Definitions](Zones/Zone-Definitions.md) -- ZoneDefinition properties
- [Shapes](Zones/Shapes.md) -- Radius and polygon shapes
- [Events](Zones/Events.md) -- Player enter/exit events
- [Player Tracking](Zones/Player-Tracking.md) -- Querying player-zone state
- [Storage](Zones/Storage.md) -- JSON and MySQL backends
- [Flag System Internals](Zones/Flag-System-Internals.md) -- Handler architecture

# BlueBeard Infrastructure Documentation

Welcome to the BlueBeard Infrastructure documentation. Each library has its own section with usage guides, API references, and implementation examples.

## Libraries

### [Core](Core/Home.md)
Foundation library: configuration management, IManager lifecycle, command framework, and utility helpers.
- [Configuration](Core/Configuration.md) -- XML config system with auto-migration
- [Commands](Core/Commands.md) -- Command tree framework with routing and permissions
- [Helpers](Core/Helpers.md) -- ThreadHelper, MessageHelper, SurfaceHelper, BarricadeHelper
- [Examples](Core/Examples.md) -- Complete plugin example

### [Cooldowns](Cooldowns/Home.md)
Centralised cooldown / timer tracking. In-memory by default with an optional MySQL-backed variant for cooldowns that must survive server restarts.
- [Getting Started](Cooldowns/Getting-Started.md) -- Setup and TryUse patterns
- [Cooldown Manager](Cooldowns/Cooldown-Manager.md) -- Full API reference and key conventions
- [Persistence](Cooldowns/Persistence.md) -- Enabling PersistentCooldownManager with BlueBeard.Database
- [Examples](Cooldowns/Examples.md) -- Dash, ability slots, per-shop restock, global lockout

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

### [Events](Events/Home.md)
Typed event bus with [Flags] enum masking. Publish and subscribe to domain events across a plugin without tight coupling between producers and consumers.
- [Getting Started](Events/Getting-Started.md) -- Setup and publish/subscribe basics
- [Event Bus](Events/Event-Bus.md) -- EventBus&lt;T&gt; and EventBusManager API reference
- [Contexts](Events/Contexts.md) -- EventContext&lt;T&gt;, the Cancelled flag, payload conventions
- [Examples](Events/Examples.md) -- Faction events, zone events, cancellation patterns

### [Holograms](Holograms/Home.md)
Proximity-based 3D hologram system with pooled UI overlays.
- [Getting Started](Holograms/Getting-Started.md) -- Setup and core concepts
- [Pools and Allocation](Holograms/Pools-and-Allocation.md) -- Global vs per-player modes
- [Dynamic Updates](Holograms/Dynamic-Updates.md) -- Runtime updates and events
- [Examples](Holograms/Examples.md) -- Shop holograms, instanced displays, filtering

### [Items](Items/Home.md)
Item state encoding and per-asset behaviour registry. Low-level byte-array helpers plus a high-level handler interface for equip / dequip / use / drop / pickup hooks.
- [Getting Started](Items/Getting-Started.md) -- Installation and when to use which subsystem
- [State Encoding](Items/State-Encoding.md) -- ItemStateEncoder API and the safety validator
- [Behaviour Registry](Items/Behaviour-Registry.md) -- ItemBehaviourManager, IItemBehaviour, ItemBehaviourBase
- [Examples](Items/Examples.md) -- Storage crate, locked medkit, ownership-veto pickups

### [MiniGames](MiniGames/Home.md)
Framework for timed, interactive mini-games layered over Unturned's effect system. Handles the full lifecycle (start → tick → input → end) so implementations only define the game logic.
- [Getting Started](MiniGames/Getting-Started.md) -- Setup, writing a handler
- [Definitions and Handlers](MiniGames/Definitions-and-Handlers.md) -- MiniGameDefinition, IMiniGameHandler reference
- [Lifecycle](MiniGames/Lifecycle.md) -- State transitions, UI precedence, completion ordering
- [Examples](MiniGames/Examples.md) -- Hotwire, reaction test, colour-match

### [SnapLogic](SnapLogic/Home.md)
Snap-point system for attaching barricades to defined positions on host barricades.
- [Getting Started](SnapLogic/Getting-Started.md) -- Setup and core concepts
- [Snap Points](SnapLogic/Snap-Points.md) -- Defining attachment points and asset filtering
- [Configuration](SnapLogic/Configuration.md) -- SnapLogicConfig options
- [Events](SnapLogic/Events.md) -- Snap lifecycle hooks
- [Examples](SnapLogic/Examples.md) -- Weapon racks, shelves, targeted snapping

### [UI](UI/Home.md)
Full-screen UI framework with generic-type-resolved screens and dialogs, plus a push-update system for external state changes.
- [Getting Started](UI/Getting-Started.md) -- Setup and architecture
- [Interfaces](UI/Interfaces.md) -- IUI, IUIScreen, IUIDialog, UIBuilder reference
- [Player State](UI/Player-State.md) -- UIContext and UIPlayerComponent
- [Event Routing](UI/Event-Routing.md) -- Button, text input, and push-update dispatch
- [Examples](UI/Examples.md) -- Multi-screen UI with pagination, dialogs, and push updates

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

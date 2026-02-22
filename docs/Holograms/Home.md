# BlueBeard.Holograms

BlueBeard.Holograms is a proximity-based hologram system for Unturned. It lets you place 3D holograms in the world that automatically show and hide UI overlays as players walk near them.

## Features

- **Proximity triggers** -- Place 3D holograms at any world position with configurable capsule-shaped trigger zones (radius and height).
- **Automatic show/hide** -- UI overlays appear when a player enters the zone and disappear when they leave. Vehicle passengers are also detected.
- **Pooled UI/Effect pairs** -- Each hologram slot pairs a screen-space UI effect ID with a world-space 3D effect ID. Pool size controls how many holograms can be visible at once.
- **Global and per-player allocation modes** -- Global mode shares pool slots across all players; per-player mode gives each player their own independent allocation.
- **Dynamic metadata updates** -- Update hologram content at runtime for a single player or for all players currently viewing a hologram.
- **Player filtering** -- Attach an optional predicate to restrict which players can see a hologram.
- **Events** -- `PlayerEnteredHologram` and `PlayerExitedHologram` fire whenever a player enters or exits a hologram zone, allowing you to hook additional logic.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Project setup, core concepts, and first hologram registration |
| [Pools and Allocation](Pools-and-Allocation.md) | How pool slots work, global vs. per-player modes, and player filtering |
| [Dynamic Updates](Dynamic-Updates.md) | Updating hologram content at runtime, unregistering, and events |
| [Examples](Examples.md) | Complete implementation examples for common use cases |

## Source Classes

| Class / Interface | Role |
|-------------------|------|
| `HologramManager` | Central manager -- registers definitions, manages pools, handles enter/exit logic |
| `HologramDefinition` | Data object describing where a hologram exists and its default metadata |
| `Hologram` | A single pool slot holding a UI effect ID and a 3D world effect ID |
| `IHologramDisplay` | Interface you implement to control what the player sees on screen |
| `HologramRegistration` | Convenience bundle for batch-registering multiple definitions with one pool and display |
| `HologramZoneComponent` | Unity MonoBehaviour that detects player/vehicle trigger enter and exit |

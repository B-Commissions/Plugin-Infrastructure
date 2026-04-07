# BlueBeard.SnapLogic

BlueBeard.SnapLogic is a reusable snap-point system for Unturned barricades. It lets you define attachment points on host barricades so that child barricades are automatically repositioned when placed nearby.

## Features

- **Named snap points** -- Define positions on a host barricade where children can attach. Each point has a name and a position offset relative to the host.
- **Automatic snapping** -- When a barricade is placed within range of a host, it is automatically moved to the nearest available snap point.
- **Asset filtering** -- Restrict which barricade types can snap to each point, or allow any barricade.
- **Rotation inheritance** -- Snapped children inherit the host barricade's rotation.
- **Events** -- Hook into snap lifecycle with `OnItemSnapped`, `OnItemUnsnapped`, `OnHostRegistered`, `OnHostDestroyed`.
- **Targeted snapping** -- Snap to a specific named point, or let the manager pick the nearest available one.
- **Salvage protection** -- Snapped children are blocked from being salvaged directly.
- **Host cleanup** -- When a host is destroyed, all children are optionally destroyed with it.
- **Debug tools** -- `/snap dump` command (debug builds only) to capture barricade positions for defining snap points.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Project setup, core concepts, and first snap definition |
| [Snap Points](Snap-Points.md) | Defining snap points, asset filtering, and positioning |
| [Configuration](Configuration.md) | SnapLogicConfig properties and defaults |
| [Events](Events.md) | Event hooks for snap lifecycle |
| [Examples](Examples.md) | Complete implementation examples |

## Source Classes

| Class / Interface | Role |
|-------------------|------|
| `SnapManager` | Central manager -- registers definitions, handles barricade events, manages snap lifecycle |
| `SnapDefinition` | Declares a barricade type as a snap host with its attachment points |
| `SnapPoint` | A single attachment position on a host barricade |
| `SnapHost` | Runtime state for a placed host barricade and its occupied points |
| `SnapAttachment` | Record of a child barricade occupying a snap point |
| `SnapLogicConfig` | Configuration defaults (snap radius, auto-register, cleanup behavior) |

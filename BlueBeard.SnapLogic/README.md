# BlueBeard.SnapLogic

Reusable snap-point system for Unturned barricades. Define attachment points on host barricades and child barricades are automatically moved into position when placed nearby.

## Features

- **Named snap points** -- Define positions on a host barricade where children can attach. Each point has a name and a position offset relative to the host.
- **Automatic snapping** -- When a barricade is placed within range of a host, it is automatically moved to the nearest available snap point.
- **Asset filtering** -- Restrict which barricade types can snap to each point, or allow any barricade.
- **Rotation inheritance** -- Snapped children inherit the host barricade's rotation.
- **Events** -- `OnItemSnapped`, `OnItemUnsnapped`, `OnHostRegistered`, `OnHostDestroyed` for hooking additional logic.
- **Targeted snapping** -- Optionally snap to a specific named point, or let the manager pick the nearest available one.
- **Salvage protection** -- Snapped children are blocked from being salvaged directly.
- **Host cleanup** -- When a host is destroyed, all children are optionally destroyed too.
- **Debug tools** -- `/snap dump` command (debug builds only) to capture barricade positions for defining snap points.

## Quick Start

```csharp
var snapManager = new SnapManager();

snapManager.RegisterDefinition(new SnapDefinition
{
    Id = "weapon_rack",
    HostAssetId = 50633,
    SnapRadius = 1.5f,
    SnapPoints = new List<SnapPoint>
    {
        new SnapPoint { Name = "slot_1", PositionOffset = new Vector3(-0.5f, 0.3f, 0f) },
        new SnapPoint { Name = "slot_2", PositionOffset = new Vector3(0.5f, 0.3f, 0f),
                         AcceptedAssetIds = new ushort[] { 50632 } }
    }
});

snapManager.OnItemSnapped += (host, attachment) =>
{
    Logger.Log($"Barricade {attachment.AssetId} snapped to {attachment.PointName}");
};

snapManager.Load();
```

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](docs/SnapLogic/Getting-Started.md) | Project setup, core concepts, and first snap definition |
| [Snap Points](docs/SnapLogic/Snap-Points.md) | Defining snap points, asset filtering, and positioning |
| [Configuration](docs/SnapLogic/Configuration.md) | SnapLogicConfig properties and defaults |
| [Events](docs/SnapLogic/Events.md) | Event hooks for snap lifecycle |
| [Examples](docs/SnapLogic/Examples.md) | Complete implementation examples |

## Source Classes

| Class / Interface | Role |
|-------------------|------|
| `SnapManager` | Central manager -- registers definitions, handles barricade events, manages snap lifecycle |
| `SnapDefinition` | Declares a barricade type as a snap host with its attachment points |
| `SnapPoint` | A single attachment position on a host barricade |
| `SnapHost` | Runtime state for a placed host barricade and its occupied points |
| `SnapAttachment` | Record of a child barricade occupying a snap point |
| `SnapLogicConfig` | Configuration defaults (snap radius, auto-register, cleanup behavior) |

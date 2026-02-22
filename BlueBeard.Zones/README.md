# BlueBeard.Zones

An advanced zone management system for Unturned with trigger-based player detection, persistent storage, a flag system for enforcing rules, block lists, and full command-line administration. Supports circular and polygon shapes. Works both as a standalone RocketMod plugin and as a library for other plugins.

## Features

- **Radius and polygon zones** with Unity trigger colliders
- **Persistent storage** via JSON files or MySQL
- **26 flags** covering damage, access, building, items, environment, notifications, effects, and group management
- **Block lists** for fine-grained item/build restrictions
- **Height bounds** for vertical zone limits
- **Zone priority** for resolving overlapping zones
- **Permission overrides** per-flag and per-zone
- **Interactive polygon builder** for creating complex zone shapes in-game
- **Console and in-game commands** for full administration
- **Vehicle support** with automatic passenger detection

## Documentation

Full documentation is available in the [docs/](docs/) folder:

- [Home](docs/Home.md) -- overview and table of contents
- [Installation](docs/Installation.md)
- [Getting Started](docs/Getting-Started.md)
- [Commands](docs/Commands.md)
- [Flags](docs/Flags.md)
- [Block Lists](docs/Block-Lists.md)
- [Permissions](docs/Permissions.md)
- [Configuration](docs/Configuration.md)
- [Developer Quick Start](docs/Developer-Quick-Start.md)
- [Zone Definitions](docs/Zone-Definitions.md)
- [Shapes](docs/Shapes.md)
- [Events](docs/Events.md)
- [Player Tracking](docs/Player-Tracking.md)
- [Storage](docs/Storage.md)
- [Flag System Internals](docs/Flag-System-Internals.md)

## Quick Start

### Installation

Drop `BlueBeard.Zones.dll` and its dependencies into your Rocket `Plugins/` folder.

### Create a Safe Zone

```
/zone create safezone 100
/zone flag add safezone noDamage
/zone flag add safezone noBuild
/zone message set safezone enter Welcome to the safe zone!
```

### Create a Polygon Zone

```
/zone node start arena
/zone node add          (walk to each corner)
/zone node add
/zone node add
/zone node finish
```

### As a Library

```csharp
var zoneManager = ZonesPlugin.Instance.ZoneManager;

zoneManager.PlayerEnteredZone += (player, zone) =>
{
    Logger.Log($"{player.channel.owner.playerID.playerName} entered {zone.Id}");
};

var definition = new ZoneDefinition
{
    Id = "my_zone",
    Center = player.Position,
    Shape = new RadiusZoneShape(50f, 30f),
    Flags = new Dictionary<string, string> { { "noDamage", "" } }
};

await zoneManager.CreateAndSaveZoneAsync(definition);
```

## Shapes

| Shape | Description | Collider |
|-------|-------------|----------|
| `RadiusZoneShape` | Cylindrical area defined by radius + height | `CapsuleCollider` |
| `PolygonZoneShape` | Arbitrary polygon extruded to a height | `MeshCollider` (convex) |

## ZoneDefinition Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Center` | `Vector3` | World position of the zone center |
| `Shape` | `IZoneShape` | The shape that defines the boundary |
| `Metadata` | `Dictionary<string, string>` | Arbitrary key-value data for your plugin |
| `Flags` | `Dictionary<string, string>` | Flag name to optional value |
| `LowerHeight` | `float?` | Lower height bound (null = unbounded) |
| `UpperHeight` | `float?` | Upper height bound (null = unbounded) |
| `Priority` | `int` | Higher wins in overlapping zones |

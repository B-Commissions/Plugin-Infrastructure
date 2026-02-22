# Zone Definitions

The `ZoneDefinition` class is the central data object for a zone. It contains all the information needed to create, persist, and manage a zone.

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Id` | `string` | -- | Unique identifier for the zone. Used in commands and API calls. |
| `Center` | `Vector3` | -- | World position of the zone center. The Unity GameObject is placed here. |
| `Shape` | `IZoneShape` | -- | Defines the zone boundary. See [Shapes](Shapes). |
| `Metadata` | `Dictionary<string, string>` | `null` | Arbitrary key-value data for your plugin's use. Not used by the flag system. |
| `Flags` | `Dictionary<string, string>` | `new()` | Flag name to optional value. See [Flags](Flags). |
| `LowerHeight` | `float?` | `null` | Lower height bound relative to center. `null` = unbounded below. |
| `UpperHeight` | `float?` | `null` | Upper height bound relative to center. `null` = unbounded above. |
| `Priority` | `int` | `0` | Higher priority zones take precedence in flag checks when zones overlap. |

## Usage

```csharp
var definition = new ZoneDefinition
{
    Id = "spawn_safezone",
    Center = new Vector3(0, 50, 0),
    Shape = new RadiusZoneShape(100f, 60f),
    Flags = new Dictionary<string, string>
    {
        { "noDamage", "" },
        { "noBuild", "weapons" },
        { "enterMessage", "Welcome to spawn!" }
    },
    LowerHeight = -20f,
    UpperHeight = 40f,
    Priority = 10
};
```

## Height Bounds

Height bounds are **relative to the zone center**. If the center is at Y=50:
- `LowerHeight = -20` means players below Y=30 are not considered in the zone
- `UpperHeight = 40` means players above Y=90 are not considered in the zone

When both are `null`, the zone has no vertical restrictions (only the collider's height matters for trigger detection).

Height bounds affect the **tracking system** (flag checks, `GetZonesForPlayer`, etc.) but not the raw trigger events. The Unity collider still fires `PlayerEnteredZone` / `PlayerExitedZone` regardless of height bounds.

## Priority

When a player is in multiple overlapping zones, the `PlayerTracker` returns zones sorted by priority (highest first). Flag handlers check the highest-priority zone first and use the first matching flag.

Example: A player is in both "global_pvp" (priority 0, `noDamage`) and "arena" (priority 10, no `noDamage` flag). The arena zone wins because it has higher priority, so damage is allowed.

## Metadata vs Flags

- **Flags** are processed by the flag enforcement system. They have specific meanings and trigger specific game behaviors.
- **Metadata** is ignored by the flag system. Use it to store arbitrary data your own plugin needs (zone type, owner, custom settings, etc.).

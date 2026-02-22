# Player Tracking

The `PlayerTracker` maintains real-time bidirectional maps of which players are in which zones. It is the recommended way to query player-zone relationships.

## Accessing the Tracker

```csharp
var tracker = ZonesPlugin.Instance.PlayerTracker;
```

## API Reference

### GetZonesForPlayer

Returns all zones a player is currently in, sorted by priority (highest first).

```csharp
List<ZoneDefinition> zones = tracker.GetZonesForPlayer(player);
```

### IsPlayerInZoneWithFlag

Checks if a player is in any zone that has a specific flag. Returns the highest-priority matching zone and the flag's value.

```csharp
if (tracker.IsPlayerInZoneWithFlag(player, "noDamage", out ZoneDefinition zone, out string flagValue))
{
    // Player is in a zone with the noDamage flag
    // zone = the zone definition
    // flagValue = the flag's value (e.g., block list name, message text, or empty string)
}
```

### GetZonesAtPosition

Returns all zones that contain a given world position, sorted by priority. Uses geometric checks (radius for circular zones, point-in-polygon for polygon zones).

```csharp
List<ZoneDefinition> zones = tracker.GetZonesAtPosition(worldPosition);
```

### IsPositionInZoneWithFlag

Checks if a position is inside any zone with a specific flag. Useful for build/damage checks where you have a position but not necessarily a player.

```csharp
if (tracker.IsPositionInZoneWithFlag(position, "noBuild", out ZoneDefinition zone, out string flagValue))
{
    // The position is inside a zone with the noBuild flag
}
```

## How It Works

The tracker subscribes to `ZoneManager.PlayerEnteredZone` and `PlayerExitedZone` events. When a player enters or exits a zone:

1. It checks height bounds (if the zone has `LowerHeight` / `UpperHeight` set)
2. It updates its internal maps:
   - `player -> set of zone IDs`
   - `zone ID -> set of players`

When a player disconnects, the tracker automatically cleans up their entries.

## Height Bounds

The tracker filters zone entries by height bounds. If a zone has height bounds set, a player must be within those bounds to be considered "in" the zone by the tracker.

This means:
- The Unity trigger collider fires `PlayerEnteredZone` regardless of height
- The tracker only records the entry if the player passes the height check
- Flag handlers use the tracker, so height bounds effectively filter flag enforcement

## Position-Based Queries

`GetZonesAtPosition` and `IsPositionInZoneWithFlag` use geometric calculations rather than the tracker's event-based maps. They check:

1. Height bounds (if set)
2. For radius zones: horizontal distance from center <= radius
3. For polygon zones: point-in-polygon test using ray casting

These methods are useful for checking positions that aren't tied to a specific player (e.g., where a barricade is being placed).

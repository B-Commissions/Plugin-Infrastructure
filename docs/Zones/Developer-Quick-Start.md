# Developer Quick Start

This guide shows how to use BlueBeard.Zones as a library from your own plugin.

## Setup

Add a project reference:

```xml
<ProjectReference Include="..\BlueBeard.Zones\BlueBeard.Zones.csproj" />
```

Access the plugin instance:

```csharp
using BlueBeard.Zones;

var plugin = ZonesPlugin.Instance;
var zoneManager = plugin.ZoneManager;
var playerTracker = plugin.PlayerTracker;
var blockListManager = plugin.BlockListManager;
```

## Creating a Zone Programmatically

### Runtime-only Zone (no persistence)

```csharp
using BlueBeard.Zones;
using BlueBeard.Zones.Shapes;
using UnityEngine;

var definition = new ZoneDefinition
{
    Id = "my_zone",
    Center = new Vector3(100, 50, 200),
    Shape = new RadiusZoneShape(radius: 25f, height: 50f),
    Metadata = new Dictionary<string, string>
    {
        { "owner", "my_plugin" }
    }
};

// This creates the zone in memory only -- it will not persist across restarts
zoneManager.CreateZone(definition);
```

### Persistent Zone

```csharp
// This creates the zone AND saves it to storage (JSON/MySQL)
await zoneManager.CreateAndSaveZoneAsync(definition);
```

### Destroying Zones

```csharp
// Runtime-only (does not delete from storage)
zoneManager.DestroyZone("my_zone");

// Persistent (destroys and deletes from storage)
await zoneManager.DestroyAndDeleteZoneAsync("my_zone");
```

## Subscribing to Events

```csharp
zoneManager.PlayerEnteredZone += (player, zone) =>
{
    if (zone.Metadata?.TryGetValue("owner", out var owner) == true && owner == "my_plugin")
    {
        // Handle zone entry for your plugin's zones
        Logger.Log($"{player.channel.owner.playerID.playerName} entered {zone.Id}");
    }
};

zoneManager.PlayerExitedZone += (player, zone) =>
{
    // Handle zone exit
};
```

## Querying Player Zones

```csharp
// Get all zones a player is currently in (sorted by priority)
var zones = playerTracker.GetZonesForPlayer(player);

// Check if a player is in a zone with a specific flag
if (playerTracker.IsPlayerInZoneWithFlag(player, "noDamage", out var zone, out var flagValue))
{
    // Player is in a zone with the noDamage flag
}

// Check if a position is in a zone with a specific flag
if (playerTracker.IsPositionInZoneWithFlag(position, "noBuild", out var zone2, out var flagValue2))
{
    // Position is in a zone with the noBuild flag
}
```

## Working with Flags

```csharp
// Add a flag to an existing zone
var zone = zoneManager.GetZone("my_zone");
zone.Flags["noDamage"] = "";
zone.Flags["enterMessage"] = "Welcome to my zone!";
await zoneManager.SaveZoneAsync(zone);

// Check flags
if (zone.Flags.ContainsKey("noDamage"))
{
    // Zone has the noDamage flag
}
```

## Working with Definitions

```csharp
// Get a specific zone
var zone = zoneManager.GetZone("my_zone");

// Get all zone definitions
var allZones = zoneManager.GetAllDefinitions();

// Access the Unity GameObject for a zone
if (zoneManager.Zones.TryGetValue("my_zone", out var gameObject))
{
    var component = gameObject.GetComponent<ZoneComponent>();
}
```

## Library-Only Mode

If you want to use BlueBeard.Zones purely as a library and handle your own flag logic, set `EnableFlagEnforcement` to `false` in the config. This disables all built-in flag handlers, but zones, events, and player tracking still work.

```xml
<ZonesConfig>
  <StorageType>json</StorageType>
  <EnableFlagEnforcement>false</EnableFlagEnforcement>
</ZonesConfig>
```

Then subscribe to events and implement your own logic:

```csharp
zoneManager.PlayerEnteredZone += (player, zone) =>
{
    if (zone.Flags.ContainsKey("myCustomFlag"))
    {
        // Your custom logic here
    }
};
```

# Flag System Internals

This page documents the internal architecture of the flag system for developers who want to understand how it works or extend it with custom flag handlers.

## Architecture Overview

```
ZonesPlugin
  └── FlagEnforcementManager
        ├── DamageFlagHandler
        ├── AccessFlagHandler
        ├── BuildFlagHandler
        ├── ItemEquipFlagHandler
        ├── LockpickFlagHandler
        ├── EnvironmentFlagHandler
        ├── NotificationFlagHandler
        ├── EffectFlagHandler
        └── GroupFlagHandler
```

The `FlagEnforcementManager` is created and loaded only when `EnableFlagEnforcement` is `true` in the config. It instantiates all flag handlers and manages their lifecycle.

## IFlagHandler Interface

```csharp
public interface IFlagHandler
{
    string FlagName { get; }
    void Subscribe();
    void Unsubscribe();
}
```

Each handler is responsible for subscribing to the appropriate Unturned events (or zone events) and enforcing its flag(s).

## FlagHandlerBase

All built-in handlers extend `FlagHandlerBase`, which provides common helpers:

```csharp
public abstract class FlagHandlerBase : IFlagHandler
{
    protected readonly PlayerTracker PlayerTracker;
    protected readonly ZoneManager ZoneManager;

    // Check if a player is in a zone with a specific flag
    protected bool IsPlayerInZoneWithFlag(Player player, string flagName,
        out ZoneDefinition zone, out string flagValue);

    // Check if a position is in a zone with a specific flag
    protected bool IsPositionInZoneWithFlag(Vector3 position, string flagName,
        out ZoneDefinition zone, out string flagValue);

    // Check if a player has override permission for a flag
    protected bool HasOverridePermission(Player player, string flagName, string zoneId = null);
}
```

## How Flag Handlers Work

### Event-based Handlers

Most handlers subscribe to Unturned delegate events. For example, the `DamageFlagHandler`:

1. Subscribes to `BarricadeManager.onDamageBarricadeRequested`, `DamageTool.damagePlayerRequested`, etc.
2. When the event fires, checks if the target position/player is in a zone with the relevant flag
3. If yes, checks for override permissions
4. If no override, sets `shouldAllow = false` to block the action

### Zone Event Handlers

Some handlers subscribe to `ZoneManager.PlayerEnteredZone` / `PlayerExitedZone`:
- `AccessFlagHandler` -- teleports players back on enter/exit
- `NotificationFlagHandler` -- sends messages
- `EffectFlagHandler` -- sends/clears effects
- `GroupFlagHandler` -- adds/removes permission groups
- `ItemEquipFlagHandler` -- dequips items on zone entry

### Coroutine Handlers

The `EnvironmentFlagHandler` runs periodic coroutines:
- Zombie cleanup (every 5 seconds) -- kills zombies in `noZombie` zones
- Generator refuel (every 10 seconds) -- refills generators in `infiniteGenerator` zones

## Handler Reference

| Handler | Flags | Unturned Hooks |
|---|---|---|
| `DamageFlagHandler` | noDamage, noPlayerDamage, noVehicleDamage, noTireDamage, noAnimalDamage, noZombieDamage, noPvP | `BarricadeManager.onDamageBarricadeRequested`, `StructureManager.onDamageStructureRequested`, `DamageTool.damagePlayerRequested`, `VehicleManager.onDamageVehicleRequested`, `VehicleManager.onDamageTireRequested`, `DamageTool.damageAnimalRequested`, `DamageTool.damageZombieRequested` |
| `AccessFlagHandler` | noEnter, noLeave, noVehicleCarjack | `ZoneManager.PlayerEnteredZone/Exited`, `VehicleManager.onEnterVehicleRequested` |
| `BuildFlagHandler` | noBuild | `BarricadeManager.onDeployBarricadeRequested`, `StructureManager.onDeployStructureRequested` |
| `ItemEquipFlagHandler` | noItemEquip | `ZoneManager.PlayerEnteredZone` |
| `LockpickFlagHandler` | noLockpick | `VehicleManager.onVehicleLockpicked` |
| `EnvironmentFlagHandler` | noZombie, noVehicleSiphoning, infiniteGenerator | `VehicleManager.onSiphonVehicleRequested`, coroutines |
| `NotificationFlagHandler` | enterMessage, leaveMessage | `ZoneManager.PlayerEnteredZone/Exited` |
| `EffectFlagHandler` | enterAddEffect, leaveAddEffect, enterRemoveEffect, leaveRemoveEffect | `ZoneManager.PlayerEnteredZone/Exited` |
| `GroupFlagHandler` | enterAddGroup, enterRemoveGroup, leaveAddGroup, leaveRemoveGroup | `ZoneManager.PlayerEnteredZone/Exited` |

## Permission Override Flow

When a flag handler checks a player:

1. Is the player in a zone with this flag? If no, allow.
2. Does the player have `zones.override.<flagName>`? If yes, allow.
3. Does the player have `zones.override.<flagName>.<zoneId>`? If yes, allow.
4. Block the action.

## Creating a Custom Flag Handler

```csharp
using BlueBeard.Zones;
using BlueBeard.Zones.Flags;
using BlueBeard.Zones.Tracking;
using SDG.Unturned;

public class MyCustomFlagHandler : FlagHandlerBase
{
    public MyCustomFlagHandler(ZoneManager zoneManager, PlayerTracker playerTracker)
        : base(zoneManager, playerTracker) { }

    public override string FlagName => "myCustomFlag";

    public override void Subscribe()
    {
        // Subscribe to relevant Unturned events
        ZoneManager.PlayerEnteredZone += OnPlayerEntered;
    }

    public override void Unsubscribe()
    {
        ZoneManager.PlayerEnteredZone -= OnPlayerEntered;
    }

    private void OnPlayerEntered(Player player, ZoneDefinition definition)
    {
        if (definition.Flags == null || !definition.Flags.ContainsKey("myCustomFlag"))
            return;

        if (HasOverridePermission(player, "myCustomFlag", definition.Id))
            return;

        // Your enforcement logic here
    }
}
```

To register it, you would need to create your own enforcement manager or manually subscribe/unsubscribe in your plugin's `Load()` / `Unload()`.

## Block List Integration

The `BuildFlagHandler` and `ItemEquipFlagHandler` use the `BlockListManager` to support per-item restrictions. When a flag has a value (the block list name), the handler checks if the specific item ID is in that block list before blocking the action.

```
Flag: noBuild = "weapons"
         │
         ▼
BlockListManager.IsItemInBlockList("weapons", barricade.id)
         │
    ┌────┴────┐
    │  true   │  false
    ▼         ▼
  BLOCK     ALLOW
```

If the flag has no value (empty string), all items are blocked.

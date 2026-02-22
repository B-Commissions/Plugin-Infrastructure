# Flags

Flags are rules applied to zones that control what players can and cannot do. Each zone can have any number of flags. Some flags accept an optional value (like a block list name or a message string).

## Adding and Removing Flags

```
/zone flag add <zoneId> <flagName> [value]
/zone flag remove <zoneId> <flagName>
/zone flag list <zoneId>
```

## Flag Reference

### Damage Flags

| Flag | Value | Description |
|---|---|---|
| `noDamage` | -- | Blocks all damage to players, vehicles, barricades, structures, animals, and zombies inside the zone. |
| `noPlayerDamage` | -- | Blocks damage dealt to players inside the zone. |
| `noVehicleDamage` | -- | Blocks damage dealt to vehicles inside the zone. |
| `noTireDamage` | -- | Blocks tire damage on vehicles inside the zone. |
| `noAnimalDamage` | -- | Blocks damage dealt to animals inside the zone. |
| `noZombieDamage` | -- | Blocks damage dealt to zombies inside the zone. |
| `noPvP` | -- | Blocks player-on-player damage. Equivalent to `noPlayerDamage` but semantically distinct. |

### Access Flags

| Flag | Value | Description |
|---|---|---|
| `noEnter` | -- | Players are teleported back when they try to enter the zone. |
| `noLeave` | -- | Players are teleported to the zone center when they try to leave. |
| `noVehicleCarjack` | -- | Prevents players from entering vehicles they don't own inside the zone. |

### Build & Item Flags

| Flag | Value | Description |
|---|---|---|
| `noBuild` | Block list name (optional) | Prevents placing barricades and structures. If a block list name is provided, only items in that list are blocked. |
| `noItemEquip` | Block list name (optional) | Forces players to dequip items when entering the zone. If a block list name is provided, only items in that list are affected. |
| `noLockpick` | -- | Prevents lockpicking vehicles inside the zone. |

### Environment Flags

| Flag | Value | Description |
|---|---|---|
| `noZombie` | -- | Automatically kills zombies that spawn inside the zone (checked every 5 seconds). |
| `noVehicleSiphoning` | -- | Prevents siphoning fuel from vehicles inside the zone. |
| `infiniteGenerator` | -- | Automatically refuels generators inside the zone to full capacity (checked every 10 seconds). |

### Notification Flags

| Flag | Value | Description |
|---|---|---|
| `enterMessage` | Message text | Displays a message to the player when they enter the zone. |
| `leaveMessage` | Message text | Displays a message to the player when they leave the zone. |

These can also be set with the shorthand commands:
```
/zone message set <zoneId> enter Welcome!
/zone message set <zoneId> leave Goodbye!
```

### Effect Flags

| Flag | Value | Description |
|---|---|---|
| `enterAddEffect` | Effect ID | Sends a visual effect to the player when they enter the zone. |
| `leaveAddEffect` | Effect ID | Sends a visual effect to the player when they leave the zone. |
| `enterRemoveEffect` | Effect ID | Clears a visual effect from the player when they enter the zone. |
| `leaveRemoveEffect` | Effect ID | Clears a visual effect from the player when they leave the zone. |

Shorthand:
```
/zone effect add <zoneId> enter <effectId>
/zone effect add <zoneId> leave <effectId>
```

### Group Flags

| Flag | Value | Description |
|---|---|---|
| `enterAddGroup` | Group name | Adds the player to a RocketMod permission group when they enter. |
| `enterRemoveGroup` | Group name | Removes the player from a RocketMod permission group when they enter. |
| `leaveAddGroup` | Group name | Adds the player to a RocketMod permission group when they leave. |
| `leaveRemoveGroup` | Group name | Removes the player from a RocketMod permission group when they leave. |

Shorthand:
```
/zone group add <zoneId> enter add <groupName>
/zone group add <zoneId> leave remove <groupName>
```

## Flag Priority

When a player is in multiple overlapping zones, the zone with the highest `Priority` value takes precedence. Flags are checked from highest-priority zone first. The first zone with a matching flag is used.

## Examples

### Safe Zone (no damage, no building)
```
/zone create safezone 100
/zone flag add safezone noDamage
/zone flag add safezone noBuild
/zone message set safezone enter You are now in the safe zone.
/zone message set safezone leave You have left the safe zone.
```

### PvE Zone (no PvP but animals and zombies can be damaged)
```
/zone create pvezone 200
/zone flag add pvezone noPvP
```

### Restricted Area (no entry without permission)
```
/zone create restricted 50
/zone flag add restricted noEnter
/zone message set restricted enter You do not have permission to be here.
```

### Generator Room (infinite fuel)
```
/zone create genroom 10
/zone flag add genroom infiniteGenerator
/zone flag add genroom noZombie
```

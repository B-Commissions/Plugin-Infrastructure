# Commands

All commands start with `/zone`. Most commands work from both the server console and in-game. Commands that require a player position (create, tp, inzone, node) are player-only.

## Zone Management

| Command | Syntax | Permission | Description |
|---|---|---|---|
| `/zone create` | `<id> <radius> [height]` | `zone.create` | Create a radius zone at your position. Player-only. |
| `/zone destroy` | `<id \| all>` | `zone.destroy` | Destroy a zone by ID, or all zones. |
| `/zone list` | | `zone.list` | List all active zones with their centers and shapes. |
| `/zone tp` | `<zoneId>` | `zone.tp` | Teleport to a zone's center. Player-only. |
| `/zone inzone` | | `zone.inzone` | Show which zones you are currently in. Player-only. |

## Polygon Builder (Node Commands)

All node commands are player-only. They manage an interactive build session for creating polygon zones.

| Command | Syntax | Permission | Description |
|---|---|---|---|
| `/zone node start` | `<id> [height]` | `zone.node.start` | Begin a polygon build session. Default height is 30. |
| `/zone node add` | | `zone.node.add` | Add your current position as a vertex. |
| `/zone node undo` | | `zone.node.undo` | Remove the last added vertex. |
| `/zone node list` | | `zone.node.list` | Show all vertices in the current session. |
| `/zone node finish` | | `zone.node.finish` | Create the polygon zone from collected vertices (minimum 3). |
| `/zone node cancel` | | `zone.node.cancel` | Discard the current build session. |

## Flag Commands

| Command | Syntax | Permission | Description |
|---|---|---|---|
| `/zone flag add` | `<zoneId> <flagName> [value]` | `zone.flag.add` | Add a flag to a zone. Value is optional. |
| `/zone flag remove` | `<zoneId> <flagName>` | `zone.flag.remove` | Remove a flag from a zone. |
| `/zone flag list` | `[zoneId]` | `zone.flag.list` | List flags on a zone, or list all available flag names. |

## Height Commands

| Command | Syntax | Permission | Description |
|---|---|---|---|
| `/zone height set` | `<zoneId> <lower> <upper>` | `zone.height.set` | Set vertical bounds relative to the zone center. |
| `/zone height remove` | `<zoneId>` | `zone.height.remove` | Remove height bounds (zone becomes vertically unbounded). |

## Block List Commands

| Command | Syntax | Permission | Description |
|---|---|---|---|
| `/zone blocklist create` | `<name>` | `zone.blocklist.create` | Create a new block list. |
| `/zone blocklist delete` | `<name>` | `zone.blocklist.delete` | Delete a block list. |
| `/zone blocklist list` | | `zone.blocklist.list` | List all block lists with item counts. |
| `/zone blocklist additem` | `<name> <itemId>` | `zone.blocklist.additem` | Add an item ID to a block list. |
| `/zone blocklist removeitem` | `<name> <itemId>` | `zone.blocklist.removeitem` | Remove an item ID from a block list. |
| `/zone blocklist items` | `<name>` | `zone.blocklist.items` | List all item IDs in a block list. |

Alias: `/zone bl` can be used instead of `/zone blocklist`.

## Message Commands

| Command | Syntax | Permission | Description |
|---|---|---|---|
| `/zone message set` | `<zoneId> <enter\|leave> <text>` | `zone.message.set` | Set an enter or leave message. |
| `/zone message remove` | `<zoneId> <enter\|leave>` | `zone.message.remove` | Remove an enter or leave message. |

Alias: `/zone msg`

## Effect Commands

| Command | Syntax | Permission | Description |
|---|---|---|---|
| `/zone effect add` | `<zoneId> <enter\|leave> <effectId>` | `zone.effect.add` | Trigger an effect when players enter or leave. |
| `/zone effect remove` | `<zoneId> <enter\|leave>` | `zone.effect.remove` | Remove an effect trigger. |

Alias: `/zone fx`

## Group Commands

| Command | Syntax | Permission | Description |
|---|---|---|---|
| `/zone group add` | `<zoneId> <enter\|leave> <add\|remove> <groupName>` | `zone.group.add` | Add/remove players to/from a RocketMod permission group on zone enter or leave. |
| `/zone group remove` | `<zoneId> <enter\|leave> <add\|remove>` | `zone.group.remove` | Remove a group action from a zone. |

Alias: `/zone grp`

### Group Command Examples

Add players to the "vip" group when they enter a zone:
```
/zone group add myzone enter add vip
```

Remove players from the "vip" group when they leave:
```
/zone group add myzone leave remove vip
```

Remove the enter action:
```
/zone group remove myzone enter add
```

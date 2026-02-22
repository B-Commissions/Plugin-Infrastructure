# Permissions

## Command Permissions

All commands require their corresponding permission. Grant them via RocketMod's permission system.

### Zone Management
| Permission | Command |
|---|---|
| `zone.create` | `/zone create` |
| `zone.destroy` | `/zone destroy` |
| `zone.list` | `/zone list` |
| `zone.tp` | `/zone tp` |
| `zone.inzone` | `/zone inzone` |

### Polygon Builder
| Permission | Command |
|---|---|
| `zone.node.start` | `/zone node start` |
| `zone.node.add` | `/zone node add` |
| `zone.node.undo` | `/zone node undo` |
| `zone.node.list` | `/zone node list` |
| `zone.node.finish` | `/zone node finish` |
| `zone.node.cancel` | `/zone node cancel` |

### Flags
| Permission | Command |
|---|---|
| `zone.flag.add` | `/zone flag add` |
| `zone.flag.remove` | `/zone flag remove` |
| `zone.flag.list` | `/zone flag list` |

### Height
| Permission | Command |
|---|---|
| `zone.height.set` | `/zone height set` |
| `zone.height.remove` | `/zone height remove` |

### Block Lists
| Permission | Command |
|---|---|
| `zone.blocklist.create` | `/zone blocklist create` |
| `zone.blocklist.delete` | `/zone blocklist delete` |
| `zone.blocklist.list` | `/zone blocklist list` |
| `zone.blocklist.additem` | `/zone blocklist additem` |
| `zone.blocklist.removeitem` | `/zone blocklist removeitem` |
| `zone.blocklist.items` | `/zone blocklist items` |

### Messages, Effects, Groups
| Permission | Command |
|---|---|
| `zone.message.set` | `/zone message set` |
| `zone.message.remove` | `/zone message remove` |
| `zone.effect.add` | `/zone effect add` |
| `zone.effect.remove` | `/zone effect remove` |
| `zone.group.add` | `/zone group add` |
| `zone.group.remove` | `/zone group remove` |

## Flag Override Permissions

Every flag can be overridden with a permission. Players with an override permission are exempt from that flag's restrictions.

### Global Override

```
zones.override.<flagName>
```

Grants immunity to a flag in **all** zones. For example:
- `zones.override.noDamage` -- player can deal damage in any zone with `noDamage`
- `zones.override.noBuild` -- player can build in any zone with `noBuild`
- `zones.override.noEnter` -- player can enter any zone with `noEnter`

### Per-Zone Override

```
zones.override.<flagName>.<zoneId>
```

Grants immunity to a flag in a **specific** zone only. For example:
- `zones.override.noBuild.safezone` -- player can build only in the zone named `safezone`
- `zones.override.noEnter.restricted` -- player can enter only the zone named `restricted`

### Examples

Allow admins to build everywhere:
```xml
<Group>
  <Id>admin</Id>
  <Permissions>
    <Permission>zones.override.noBuild</Permission>
    <Permission>zones.override.noDamage</Permission>
  </Permissions>
</Group>
```

Allow VIPs to enter the restricted zone:
```xml
<Group>
  <Id>vip</Id>
  <Permissions>
    <Permission>zones.override.noEnter.restricted</Permission>
  </Permissions>
</Group>
```

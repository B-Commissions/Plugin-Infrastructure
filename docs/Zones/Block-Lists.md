# Block Lists

Block lists are named collections of item IDs. They are used in combination with the `noBuild` and `noItemEquip` flags to restrict specific items rather than blocking everything.

## Creating and Managing Block Lists

```
/zone blocklist create <name>         -- create a new block list
/zone blocklist delete <name>         -- delete a block list
/zone blocklist list                  -- list all block lists
/zone blocklist additem <name> <id>   -- add an item ID
/zone blocklist removeitem <name> <id> -- remove an item ID
/zone blocklist items <name>          -- show all items in a list
```

## Using Block Lists with Flags

When you add a flag like `noBuild` or `noItemEquip`, you can pass a block list name as the flag value. Only items in that block list will be affected.

### Example: Block specific barricades

```
/zone blocklist create military_items
/zone blocklist additem military_items 363
/zone blocklist additem military_items 519
/zone blocklist additem military_items 1244

/zone flag add myzone noBuild military_items
```

Now in `myzone`, players cannot place items 363, 519, or 1244, but all other building is allowed.

### Example: Prevent equipping specific weapons

```
/zone blocklist create banned_weapons
/zone blocklist additem banned_weapons 1037
/zone blocklist additem banned_weapons 297

/zone flag add myzone noItemEquip banned_weapons
```

Players entering `myzone` with items 1037 or 297 equipped will have them dequipped.

### Without a Block List

If you add `noBuild` or `noItemEquip` without a block list name, **all** items are blocked:

```
/zone flag add myzone noBuild
```

This prevents placing any barricades or structures in the zone.

## Storage

Block lists are persisted alongside zones. If you use JSON storage, they are saved in `zones.json`. If you use MySQL, they are stored in the `bb_zone_blocklists` table.

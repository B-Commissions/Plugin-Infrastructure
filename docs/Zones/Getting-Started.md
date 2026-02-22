# Getting Started

This page walks through common tasks for server administrators.

## Creating Your First Zone

### Radius Zone

Stand at the center of where you want the zone and run:

```
/zone create myzone 50
```

This creates a zone named `myzone` with a 50-unit radius and the default height of 30 units. You can specify a custom height:

```
/zone create myzone 50 100
```

### Polygon Zone

Polygon zones let you define irregular shapes by walking to each vertex.

1. Start a build session:
   ```
   /zone node start mypolygon
   ```
   Optionally set a height: `/zone node start mypolygon 50`

2. Walk to the first corner of your zone and run:
   ```
   /zone node add
   ```

3. Walk to the next corner and repeat `/zone node add`. You need at least 3 nodes.

4. Review your nodes at any time:
   ```
   /zone node list
   ```

5. Made a mistake? Remove the last node:
   ```
   /zone node undo
   ```

6. When you're happy with the shape, finish:
   ```
   /zone node finish
   ```

7. To discard the session without creating a zone:
   ```
   /zone node cancel
   ```

## Adding Flags

Flags control what players can and cannot do inside a zone. For example, to make a safe zone:

```
/zone flag add myzone noDamage
/zone flag add myzone noBuild
/zone message set myzone enter Welcome to the safe zone!
/zone message set myzone leave You have left the safe zone.
```

See [Flags](Flags) for the full list.

## Listing and Managing Zones

```
/zone list              -- show all active zones
/zone inzone            -- show which zones you're currently in
/zone tp myzone         -- teleport to a zone's center
/zone destroy myzone    -- delete a zone
/zone destroy all       -- delete all zones
```

## Setting Height Bounds

By default, zones extend infinitely vertically through the collider. You can add logical height bounds for the tracking system:

```
/zone height set myzone -10 50
```

This means players are only considered "in the zone" between 10 units below and 50 units above the zone center. Remove bounds with:

```
/zone height remove myzone
```

## Using Block Lists

Block lists let you restrict specific items rather than blocking everything. For example, to only prevent placing certain barricades:

```
/zone blocklist create weapons
/zone blocklist additem weapons 363
/zone blocklist additem weapons 519
/zone flag add myzone noBuild weapons
```

Now only items 363 and 519 are blocked from being placed in `myzone`. All other building is allowed.

See [Block Lists](Block-Lists) for details.

# Pools and Allocation

This page explains how hologram pools work, the difference between global and per-player allocation modes, and how player filtering restricts visibility.

## What Is a Pool?

A pool is a `List<Hologram>` -- a fixed set of UI/Effect pairs that can be assigned to players as they enter hologram zones. Each `Hologram` in the list is one slot:

```csharp
var pool = new List<Hologram>
{
    new() { UI = 50600, Effect = 50601 },  // Slot 0
    new() { UI = 50602, Effect = 50603 },  // Slot 1
    new() { UI = 50604, Effect = 50605 },  // Slot 2
};
```

**Pool size determines the maximum number of simultaneously visible holograms.** When a player enters a hologram zone, the manager finds the first unused slot in the pool, assigns it to that player-definition pair, and sends the corresponding UI and 3D effect. If no slot is available, the hologram is not shown and a warning is logged.

Multiple definitions can share the same pool (and the same `IHologramDisplay`). When you register definitions using `RegisterDefinition` with the same pool reference, or via a `HologramRegistration`, all those definitions draw from the same pool of slots.

## Allocation Modes

The `IsGlobal` flag controls how "used" slots are tracked.

### Global Mode (`IsGlobal = true`)

In global mode, a single `HashSet<int>` of used slot indices is shared across **all players**. When any player occupies slot 0, no other player can use slot 0 for any definition registered to that pool.

```
Pool: [Slot 0] [Slot 1] [Slot 2]

Player A enters Zone X -> assigned Slot 0
Player B enters Zone Y -> assigned Slot 1 (Slot 0 is taken)
Player C enters Zone Z -> assigned Slot 2 (Slots 0, 1 are taken)
Player D enters Zone W -> POOL EXHAUSTED (all slots in use)
```

**When to use global mode:**
- Unique world objects where each hologram location needs a dedicated effect slot.
- Situations where you have one pool slot per hologram definition (1:1 mapping).
- Preventing visual conflicts when different players at different locations must not share the same effect ID.

When a player disconnects, their global slot reservations are freed automatically.

### Per-Player Mode (`IsGlobal = false`)

In per-player mode, each player maintains their **own** `HashSet<int>` of used slot indices. Slot 0 being used by Player A has no effect on Player B's availability.

```
Pool: [Slot 0] [Slot 1] [Slot 2]

Player A enters Zone X -> assigned Slot 0 (from A's own tracker)
Player B enters Zone X -> assigned Slot 0 (from B's own tracker -- independent)
Player A enters Zone Y -> assigned Slot 1 (from A's own tracker)
Player B enters Zone Y -> assigned Slot 1 (from B's own tracker)
```

**When to use per-player mode:**
- Instanced content where every player should see the same holograms independently.
- Personal displays (stats, quests, inventories) that are unique to each player.
- Situations where many players will be near the same holograms and you want them all to see the content.

### Choosing a Pool Size

| Scenario | Mode | Recommended Pool Size |
|----------|------|-----------------------|
| 5 shop locations, global | Global | At least 5 (one slot per definition) |
| 5 shop locations, instanced | Per-Player | At least 5 (one slot per definition the player can see simultaneously) |
| 1 personal stats display | Per-Player | 1 is sufficient if only one can be viewed at a time |
| 10 world markers, rarely near more than 3 at once | Global | 3-4 slots may be enough |

If pool exhaustion occurs, the manager logs a warning: `[HologramManager] Pool exhausted for zone at {position}`.

## Slot Assignment Lifecycle

1. **Player enters zone** -- The manager iterates the pool from index 0 upward, finds the first unused index, and assigns it.
2. **UI and Effect sent** -- `EffectManager.sendEffect` spawns the 3D visual at the definition's position. `EffectManager.sendUIEffect` sends the screen overlay. `IHologramDisplay.Show` is called.
3. **Player exits zone** -- `IHologramDisplay.Hide` is called. The 3D effect is cleared via `EffectManager.askEffectClearByID`. The slot index is released back to the used-index set.
4. **Player disconnects** -- All per-player state is removed. If global mode, the player's global slot reservations are released.

## Player Filtering

You can attach an optional `Func<Player, bool>` predicate when registering a definition. The predicate is evaluated when a player enters the trigger zone. If it returns `false`, the hologram is not shown to that player.

```csharp
hologramManager.RegisterDefinition(definition, display, pool, isGlobal: false,
    playerFilter: player => player.quests.isMemberOfSameGroupAs(someOtherPlayer));
```

The filter is checked **once** at zone entry. If a player's state changes while they are inside the zone (e.g., they leave a group), the hologram remains visible until they physically exit and re-enter.

### Filter Examples

```csharp
// Only admins
playerFilter: player => player.channel.owner.isAdmin

// Only players with enough health
playerFilter: player => player.life.health > 50

// Only players in a specific Steam group or permission group
playerFilter: player => HasPermission(player, "vip")

// No filter (default) -- all players see the hologram
playerFilter: null
```

Note that `playerFilter` is only available via the `RegisterDefinition` method. The `HologramRegistration` batch registration class does not expose a player filter; if you need filtering with batch registration, register each definition individually.

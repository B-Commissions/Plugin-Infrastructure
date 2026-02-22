# Dynamic Updates

This page covers how to update hologram content at runtime, remove holograms, and react to player enter/exit events.

## Updating a Single Player

Use `UpdatePlayer` to refresh the hologram display for one specific player. This merges the provided metadata into the player's existing metadata for that definition, then calls `IHologramDisplay.Show` again with the updated values.

```csharp
hologramManager.UpdatePlayer(player, definition, new Dictionary<string, string>
{
    { "stock", "42" },
    { "discount", "20%" }
});
```

**Behavior:**
- The new key-value pairs are merged into the player's per-instance metadata dictionary. Existing keys are overwritten; keys not present in the update are preserved.
- `IHologramDisplay.Show` is called with the merged metadata, allowing your display to refresh the UI.
- If the player is not currently viewing the specified definition (they are outside the zone or not assigned a slot), the call is silently ignored.

### Method Signature

```csharp
public void UpdatePlayer(Player player, HologramDefinition definition,
    Dictionary<string, string> metadata)
```

## Updating All Players

Use `UpdateAll` to refresh the hologram for every player who is currently viewing it. This is useful for broadcasting state changes like stock count updates or status changes.

```csharp
hologramManager.UpdateAll(definition, new Dictionary<string, string>
{
    { "stock", "0" },
    { "status", "SOLD OUT" }
});
```

**Behavior:**
- The definition's own `Metadata` dictionary is updated first (if it is not null), so future players entering the zone will see the new values.
- Then `UpdatePlayer` is called for every player who currently has an assigned slot for this definition.
- Players who are not in the zone are unaffected.

### Method Signature

```csharp
public void UpdateAll(HologramDefinition definition,
    Dictionary<string, string> metadata)
```

## Unregistering a Definition

Use `UnregisterDefinition` to completely remove a hologram from the system.

```csharp
hologramManager.UnregisterDefinition(definition);
```

**What happens:**
1. For every player currently assigned to this definition:
   - `IHologramDisplay.Hide` is called to clean up the UI.
   - `EffectManager.askEffectClearByID` removes the 3D world effect.
   - The pool slot is released (from either the global or per-player used-index set).
   - The player's assignment and metadata for this definition are removed.
2. The Unity `GameObject` containing the trigger zone collider is destroyed.
3. The definition is removed from the internal registration dictionary.

After unregistering, the definition can be re-registered with `RegisterDefinition` if needed.

### Method Signature

```csharp
public void UnregisterDefinition(HologramDefinition definition)
```

## Events

`HologramManager` exposes two events that fire when players enter or exit hologram zones.

### PlayerEnteredHologram

Fires after a player has been assigned a pool slot and the display's `Show` method has been called.

```csharp
hologramManager.PlayerEnteredHologram += (player, definition) =>
{
    Logger.Log($"{player.channel.owner.playerID.playerName} entered hologram at {definition.Position}");
};
```

**Signature:**

```csharp
public event Action<Player, HologramDefinition> PlayerEnteredHologram;
```

**When it fires:**
- After the pool slot is assigned, the 3D effect is spawned, the UI effect is sent, and `IHologramDisplay.Show` completes.
- It does **not** fire if the pool is exhausted (no slot available).
- It does **not** fire if the player filter rejects the player.

### PlayerExitedHologram

Fires after a player's hologram has been hidden and their pool slot released.

```csharp
hologramManager.PlayerExitedHologram += (player, definition) =>
{
    Logger.Log($"{player.channel.owner.playerID.playerName} exited hologram at {definition.Position}");
};
```

**Signature:**

```csharp
public event Action<Player, HologramDefinition> PlayerExitedHologram;
```

**When it fires:**
- After `IHologramDisplay.Hide` is called, the 3D effect is cleared, and the slot index is released.
- It does **not** fire on player disconnect. Disconnect cleanup frees slots silently.
- It does **not** fire when `UnregisterDefinition` hides holograms. That cleanup is handled internally without raising events.

### Typical Event Use Cases

- **Logging and analytics** -- Track how often players interact with hologram zones.
- **Sound effects** -- Play an audio cue when a player enters a hologram area.
- **Chained logic** -- Trigger other game systems (quests, achievements) based on hologram proximity.
- **Cooldowns** -- Track when a player last exited a hologram to implement re-entry delays.

## Query Methods

`HologramManager` provides two read-only methods for inspecting current state:

### GetRegisteredDefinitions

Returns all currently registered `HologramDefinition` instances.

```csharp
foreach (var def in hologramManager.GetRegisteredDefinitions())
{
    Logger.Log($"Hologram at {def.Position}");
}
```

### GetPlayerAssignments

Returns all current player-to-definition assignments as `(Player, HologramDefinition)` tuples.

```csharp
foreach (var (player, def) in hologramManager.GetPlayerAssignments())
{
    Logger.Log($"{player.channel.owner.playerID.playerName} is viewing hologram at {def.Position}");
}
```

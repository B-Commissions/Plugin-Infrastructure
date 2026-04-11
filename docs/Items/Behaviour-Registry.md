# Behaviour Registry

`ItemBehaviourManager` is the registry that maps asset IDs to `IItemBehaviour` implementations. It dispatches equip and dequip events automatically via `PlayerEquipment.OnUseableChanged_Global`; "use", "drop", and "pickup" must be dispatched manually because Unturned does not expose universal hooks for those.

## IItemBehaviour

```csharp
public interface IItemBehaviour
{
    void OnEquipped(Player player, ItemJar jar);
    void OnDequipped(Player player, ItemJar jar);
    void OnUsed(Player player, ItemJar jar);
    void OnDropped(Player player, ItemJar jar);
    bool OnPickedUp(Player player, ItemJar jar);
}
```

Every method except `OnPickedUp` returns `void`. `OnPickedUp` returns `bool` so a handler can advise the caller to veto the pickup; enforcement is the caller's responsibility (see `NotifyPickedUp`).

## ItemBehaviourBase

Abstract base with virtual no-op implementations. Inherit and override only what you need:

```csharp
public class MedkitBehaviour : ItemBehaviourBase
{
    public override void OnEquipped(Player player, ItemJar jar)
    {
        // Only OnEquipped is overridden; every other hook stays the base no-op.
    }
}
```

`OnPickedUp` in the base returns `true` (allow).

## ItemBehaviourManager

### Lifecycle

```csharp
var mgr = new ItemBehaviourManager();
mgr.Register(1200, new MedkitBehaviour());
mgr.Load();    // subscribes to PlayerEquipment.OnUseableChanged_Global
// ...
mgr.Unload();  // unsubscribes
```

### Public API

| Method | Purpose |
|--------|---------|
| `Register(ushort assetId, IItemBehaviour behaviour)` | Attach a handler to an asset ID. Overwrites any existing handler for the same ID. |
| `Unregister(ushort assetId)` | Remove the handler for an asset ID. |
| `GetBehaviour(ushort assetId)` | Lookup and return the registered handler, or `null`. |
| `NotifyUsed(Player, ItemJar)` | Manually dispatch `OnUsed` to the registered handler (if any). |
| `NotifyDropped(Player, ItemJar)` | Manually dispatch `OnDropped`. |
| `NotifyPickedUp(Player, ItemJar)` | Manually dispatch `OnPickedUp`; returns the handler's decision (true = allow). |

### Automatic hooks

- **Equip** -- Fires on `PlayerEquipment.OnUseableChanged_Global` when the new useable's asset id has a registered handler. The `ItemJar` is resolved by looking up the equipped page/slot at the time of the callback.
- **Dequip** -- Detected by diffing the current useable asset id against the previously-seen id for the player. The `ItemJar` is null (the original jar is no longer reachable by the time the event fires -- handlers that need the jar reference should capture it in `OnEquipped`).

Per-player state for the equip/dequip diff is tracked in an internal dictionary keyed by Steam ID, populated on player connect and cleaned up on disconnect.

### Manual hooks

`OnUsed`, `OnDropped`, `OnPickedUp` are not dispatched automatically. Unturned exposes per-useable hooks (e.g. `UseableConsumeable.onPerformingAid`) but no universal "item was used" event that maps cleanly to asset IDs. Instead, call the `Notify*` helpers from wherever your plugin detects the action:

```csharp
// Example: hook into a custom interaction and notify on use:
public void OnInteractWithStorageCrate(UnturnedPlayer player, ItemJar jar)
{
    MyPlugin.Items.NotifyUsed(player.Player, jar);
}

// Example: vetoing a pickup from a zone-locked area:
var allowed = MyPlugin.Items.NotifyPickedUp(player, jar);
if (!allowed)
{
    // Caller enforces the veto (return the item to the ground, reject the pickup, etc.)
    RejectPickup(player, jar);
}
```

### Error isolation

Handlers that throw propagate the exception back to the caller of `Notify*` (or to Unturned's event loop for the automatic hooks). Wrap your handler body in a try/catch if you want to isolate failures.

### One handler per asset ID

Registering a second handler for an existing asset ID overwrites the first. If you need composite behaviour (multiple plugins extending the same item), compose them explicitly:

```csharp
public class CompositeBehaviour : ItemBehaviourBase
{
    private readonly List<IItemBehaviour> _delegates = new();
    public void Add(IItemBehaviour b) => _delegates.Add(b);

    public override void OnEquipped(Player player, ItemJar jar)
    {
        foreach (var d in _delegates) d.OnEquipped(player, jar);
    }
    // ... etc for each hook
}
```

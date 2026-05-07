# Behaviour Registry

BlueBeard ships per-asset behaviour managers for six SDG entity types. They all share the same shape: register one behaviour per key, and the manager auto-dispatches the events Unturned exposes globally and provides `Notify*` helpers for the rest.

| Manager | Key type | Asset / entity | Auto-dispatched events | Manual `Notify*` |
|---------|----------|----------------|------------------------|-------------------|
| `ItemBehaviourManager` | `ushort` asset id | `ItemJar` | Equip, dequip | Used, Dropped, PickedUp |
| `BarricadeBehaviourManager` | `ushort` asset id | `BarricadeDrop` | Spawned, damage, salvage | Destroyed |
| `StructureBehaviourManager` | `ushort` asset id | `StructureDrop` | Damage | Spawned, Destroyed |
| `VehicleBehaviourManager` | `ushort` asset id | `InteractableVehicle` | Enter, damage, tire damage, siphon, lockpick | Destroyed, Exited |
| `ZombieBehaviourManager` | `byte` zombie type | `Zombie` | Damage | Spawned, Killed |
| `AnimalBehaviourManager` | `ushort` asset id | `Animal` | Damage | Spawned, Killed |

All six derive from `EntityBehaviourRegistry<TKey, TBehaviour>` (in `BlueBeard.Items.Behaviours`), which provides the shared `Register` / `Unregister` / `GetBehaviour` / `TryGet` / `All` API and the `IManager` lifecycle. Concrete managers wire their entity-specific SDG events.

## The shared registry pattern

Every manager exposes the same identity-shaped surface:

| Method | Purpose |
|--------|---------|
| `Register(key, behaviour)` | Attach a behaviour. Overwrites any existing entry for `key`. Throws `ArgumentNullException` if `behaviour` is null. |
| `Unregister(key)` | Remove the behaviour for `key`. No-op if none was registered. |
| `GetBehaviour(key)` | Lookup; returns the behaviour or `null`. |
| `TryGet(key, out behaviour)` | Same as above with the standard `TryGet` shape. |
| `All` | Read-only view of all registered entries. |
| `Load()` | Subscribe to the manager's SDG events. Call once on plugin load. |
| `Unload()` | Unsubscribe and clear all entries. Call on plugin unload. |

Behaviours that need composite handlers (multiple plugins reacting to the same key) compose explicitly — see [Items example](#items) below.

---

## Items

```csharp
public interface IItemBehaviour
{
    void OnEquipped(Player player, ItemJar jar);
    void OnDequipped(Player player, ItemJar jar);
    void OnUsed(Player player, ItemJar jar);     // manual via NotifyUsed
    void OnDropped(Player player, ItemJar jar);  // manual via NotifyDropped
    bool OnPickedUp(Player player, ItemJar jar); // manual via NotifyPickedUp; false = veto
}
```

Inherit `ItemBehaviourBase` for virtual no-op defaults (`OnPickedUp` defaults to `true`).

### Auto hooks

- **Equip** fires on `PlayerEquipment.OnUseableChanged_Global`. The `ItemJar` is resolved by looking up the equipped page/slot at the time of the callback.
- **Dequip** is detected by diffing the current useable asset id against the previously-seen id per player. The `ItemJar` is `null` (the original jar is no longer reachable by the time the event fires — handlers that need the jar reference should capture it in `OnEquipped`).

### Manual hooks

`NotifyUsed`, `NotifyDropped`, `NotifyPickedUp` exist because Unturned doesn't expose a universal asset-id-keyed event for these. Call them from your own hook (a useable override, an interaction handler, an inventory event):

```csharp
MyPlugin.Items.NotifyUsed(player, itemJar);

if (!MyPlugin.Items.NotifyPickedUp(player, jar))
{
    // Caller enforces the veto
    RejectPickup(player, jar);
}
```

### Composite

```csharp
public class CompositeItemBehaviour : ItemBehaviourBase
{
    private readonly List<IItemBehaviour> _delegates = new();
    public void Add(IItemBehaviour b) => _delegates.Add(b);

    public override void OnEquipped(Player p, ItemJar j) { foreach (var d in _delegates) d.OnEquipped(p, j); }
    public override void OnDequipped(Player p, ItemJar j) { foreach (var d in _delegates) d.OnDequipped(p, j); }
    public override void OnUsed(Player p, ItemJar j) { foreach (var d in _delegates) d.OnUsed(p, j); }
    public override void OnDropped(Player p, ItemJar j) { foreach (var d in _delegates) d.OnDropped(p, j); }
    public override bool OnPickedUp(Player p, ItemJar j)
    {
        foreach (var d in _delegates) if (!d.OnPickedUp(p, j)) return false;
        return true;
    }
}
```

---

## Barricades

```csharp
public interface IBarricadeBehaviour
{
    void OnSpawned(BarricadeDrop drop);
    bool OnDamageRequested(CSteamID instigator, BarricadeDrop drop, ushort pendingDamage, EDamageOrigin damageOrigin);
    bool OnSalvageRequested(BarricadeDrop drop, SteamPlayer instigator);
    void OnDestroyed(BarricadeDrop drop); // manual via NotifyDestroyed
}
```

Inherit `BarricadeBehaviourBase` for virtual no-op defaults (damage and salvage default to allow).

### Auto hooks

- **Spawned** — `BarricadeManager.onBarricadeSpawned`
- **Damage** — `BarricadeManager.onDamageBarricadeRequested`. Returning `false` from the handler sets the SDG event's `shouldAllow` to false.
- **Salvage** — `BarricadeDrop.OnSalvageRequested_Global`. Returning `false` sets `shouldAllow` to false.

### Manual

```csharp
// From your own destroy hook (e.g. a barricade-tracking subsystem):
MyPlugin.Barricades.NotifyDestroyed(drop);
```

### Quick example

```csharp
public class CrateOwnershipBehaviour : BarricadeBehaviourBase
{
    public override bool OnSalvageRequested(BarricadeDrop drop, SteamPlayer instigator)
    {
        // Only the owner can salvage
        return drop.GetServersideData().owner == instigator.playerID.steamID.m_SteamID;
    }
}

MyPlugin.Barricades.Register(10500, new CrateOwnershipBehaviour());
```

---

## Structures

```csharp
public interface IStructureBehaviour
{
    void OnSpawned(StructureDrop drop);    // manual via NotifySpawned
    bool OnDamageRequested(CSteamID instigator, StructureDrop drop, ushort pendingDamage, EDamageOrigin damageOrigin);
    void OnDestroyed(StructureDrop drop);  // manual via NotifyDestroyed
}
```

### Auto hooks

- **Damage** — `StructureManager.onDamageStructureRequested`. Returning `false` denies.

### Manual

`NotifySpawned` and `NotifyDestroyed` — Unturned doesn't expose globally-keyed events for these.

```csharp
// From your own deploy hook:
MyPlugin.Structures.NotifySpawned(drop);
```

---

## Vehicles

```csharp
public interface IVehicleBehaviour
{
    bool OnEnterRequested(Player player, InteractableVehicle vehicle);
    bool OnDamageRequested(CSteamID instigator, InteractableVehicle vehicle, ushort pendingDamage, bool canRepair, EDamageOrigin damageOrigin);
    bool OnTireDamageRequested(CSteamID instigator, InteractableVehicle vehicle, int tireIndex, EDamageOrigin damageOrigin);
    bool OnSiphonRequested(InteractableVehicle vehicle, Player instigator, ushort desiredAmount);
    bool OnLockpickRequested(InteractableVehicle vehicle, Player instigator);
    void OnDestroyed(InteractableVehicle vehicle); // manual
    void OnExited(Player player, InteractableVehicle vehicle); // manual
}
```

### Auto hooks

| Hook | SDG event |
|------|-----------|
| `OnEnterRequested` | `VehicleManager.onEnterVehicleRequested` |
| `OnDamageRequested` | `VehicleManager.onDamageVehicleRequested` |
| `OnTireDamageRequested` | `VehicleManager.onDamageTireRequested` |
| `OnSiphonRequested` | `VehicleManager.onSiphonVehicleRequested` |
| `OnLockpickRequested` | `VehicleManager.onVehicleLockpicked` |

All return `false` to deny.

### Manual

```csharp
MyPlugin.Vehicles.NotifyDestroyed(vehicle);
MyPlugin.Vehicles.NotifyExited(player, vehicle);
```

### Quick example — VIP vehicle (only the owner may enter)

```csharp
public class VipVehicleBehaviour : VehicleBehaviourBase
{
    public override bool OnEnterRequested(Player player, InteractableVehicle vehicle)
    {
        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        return vehicle.lockedOwner.m_SteamID == steamId;
    }
}

MyPlugin.Vehicles.Register(95, new VipVehicleBehaviour());
```

---

## Zombies

Keyed by **zombie type** (`Zombie.type`, a `byte`) rather than asset id, since zombies don't have asset IDs in the same sense as items.

```csharp
public interface IZombieBehaviour
{
    bool OnDamageRequested(ref DamageZombieParameters parameters); // false = deny
    void OnKilled(Zombie zombie);   // manual
    void OnSpawned(Zombie zombie);  // manual
}
```

### Auto hooks

- **Damage** — `DamageTool.damageZombieRequested`. Returning `false` denies. `parameters` is passed by ref so handlers can mutate damage values before the event continues.

### Manual

```csharp
MyPlugin.Zombies.NotifySpawned(zombie);
MyPlugin.Zombies.NotifyKilled(zombie);
```

---

## Animals

```csharp
public interface IAnimalBehaviour
{
    bool OnDamageRequested(ref DamageAnimalParameters parameters); // false = deny
    void OnKilled(Animal animal);   // manual
    void OnSpawned(Animal animal);  // manual
}
```

### Auto hooks

- **Damage** — `DamageTool.damageAnimalRequested`. Returning `false` denies.

---

## Lifecycle and threading

All managers implement `IManager`. Wire them up in your plugin's `Load`:

```csharp
public class MyPlugin : RocketPlugin
{
    public static ItemBehaviourManager Items { get; private set; }
    public static BarricadeBehaviourManager Barricades { get; private set; }
    public static VehicleBehaviourManager Vehicles { get; private set; }

    protected override void Load()
    {
        Items = new ItemBehaviourManager();
        Barricades = new BarricadeBehaviourManager();
        Vehicles = new VehicleBehaviourManager();

        // Register your behaviours
        Items.Register(1200, new MedkitBehaviour());
        Barricades.Register(10500, new CrateOwnershipBehaviour());
        Vehicles.Register(95, new VipVehicleBehaviour());

        // Subscribe to SDG events
        Items.Load();
        Barricades.Load();
        Vehicles.Load();
    }

    protected override void Unload()
    {
        Vehicles.Unload();
        Barricades.Unload();
        Items.Unload();
    }
}
```

Auto-dispatched events run on Unturned's main thread (the only thread SDG events fire from); behaviours can touch the world directly without dispatching. `Notify*` helpers run synchronously on the calling thread — call them on the main thread or marshal first.

## Error isolation

Behaviour exceptions propagate to the caller (or to Unturned's event loop for auto hooks). Wrap your behaviour body in `try/catch` if you want a single failing handler not to break the chain.

## One behaviour per key

`Register` overwrites any existing entry for the same key. To stack multiple handlers, compose them explicitly (see the items composite example above) — the same pattern works for any of the entity types.

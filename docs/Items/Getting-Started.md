# Getting Started

## Installation

Add a project reference in your plugin's `.csproj`:

```xml
<ProjectReference Include="..\BlueBeard.Items\BlueBeard.Items.csproj" />
```

BlueBeard.Items depends on `BlueBeard.Core` (pulled in automatically).

## When to use which subsystem

| Need | Use |
|------|-----|
| Store custom data inside an item / barricade / vehicle state byte array, with explicit offsets | `ItemStateEncoder` |
| Same as above, but track byte position automatically | `StateWriter` / `StateReader` |
| Check whether it's safe to encode custom state on a particular item asset | `ItemStateValidator` |
| React to a player equipping, dequipping, using, dropping, or picking up a specific item | `ItemBehaviourManager` + `IItemBehaviour` |
| React to a barricade being spawned / damaged / salvaged | `BarricadeBehaviourManager` + `IBarricadeBehaviour` |
| React to a structure being damaged | `StructureBehaviourManager` + `IStructureBehaviour` |
| React to a vehicle being entered / damaged / siphoned / lockpicked | `VehicleBehaviourManager` + `IVehicleBehaviour` |
| React to a zombie / animal being damaged | `ZombieBehaviourManager` / `AnimalBehaviourManager` |

The subsystems are independent — use any combination.

## State encoder in 60 seconds (cursor API)

```csharp
using BlueBeard.Items;

if (ItemStateValidator.IsSafeForCustomState(myAssetId))
{
    jar.item.state = new StateWriter(18)
        .WriteUInt64(ownerSteamId)        // bytes 0..7
        .WriteBool(isLocked)              // byte 8
        .WriteUInt16(chargesRemaining)    // bytes 9..10
        .WriteUInt32(unlockedAt)          // bytes 11..14
        .ToArray();
}
```

Read it back later:

```csharp
var r = new StateReader(jar.item.state);
var ownerSteamId = r.ReadUInt64();
var isLocked     = r.ReadBool();
var charges      = r.ReadUInt16();
```

Or use the offset-based static API if you prefer explicit offsets — see [State Encoding](State-Encoding.md).

## Behaviour registries in 60 seconds

The same pattern works for all six entity types. Items example:

```csharp
using BlueBeard.Items;
using BlueBeard.Items.Behaviours;

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

        Items.Register(1200, new MedkitBehaviour());
        Barricades.Register(10500, new CrateOwnershipBehaviour());
        Vehicles.Register(95, new VipVehicleBehaviour());

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

public class MedkitBehaviour : ItemBehaviourBase
{
    public override void OnEquipped(Player player, ItemJar jar)
    {
        ChatManager.serverSendMessage("You raised the medkit.", Color.green, toPlayer: player.channel.owner);
    }
}
```

Auto-dispatched events fire as soon as the entity-specific SDG hook fires (item equip, barricade damage, vehicle enter, etc.). Events that aren't globally exposed by Unturned use `Notify*` helpers — call them from your own hook.

## Quick reference

### Encoder

| API | Purpose |
|-----|---------|
| `new StateWriter(size)` / `new StateReader(buf)` | Cursor wrappers (recommended) |
| `ItemStateEncoder.Write*` / `Read*` | Static, offset-based (back-compat) |
| `ItemStateValidator.IsSafeForCustomState(asset)` | Returns false for weapons/attachments |

### Behaviour managers (same shape on all six)

| API | Purpose |
|-----|---------|
| `Register(key, behaviour)` | Attach a handler |
| `Unregister(key)` | Detach |
| `GetBehaviour(key)` / `TryGet(key, out)` | Lookup |
| `Load()` / `Unload()` | Subscribe / unsubscribe SDG events |
| `Notify*` | Manual dispatch for events Unturned doesn't globally expose |

See [Behaviour Registry](Behaviour-Registry.md) for the per-entity interface details.

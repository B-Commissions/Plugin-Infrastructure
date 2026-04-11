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
| Store custom data inside an item's state bytes that travels with the item | `ItemStateEncoder` |
| React to a player equipping, dequipping, using, dropping, or picking up a specific item | `ItemBehaviourManager` + `IItemBehaviour` |
| Check whether it's safe to encode custom state on a particular asset type | `ItemStateValidator` |

The two subsystems are independent -- you can use either one in isolation.

## State encoder in 60 seconds

```csharp
using BlueBeard.Items;

// Always validate first — weapons and attachments are NOT safe.
if (ItemStateValidator.IsSafeForCustomState(myAssetId))
{
    var state = new byte[18];
    ItemStateEncoder.WriteUInt64(state, 0, ownerSteamId);   // bytes 0..7
    ItemStateEncoder.WriteBool(state, 8, isLocked);         // byte 8
    ItemStateEncoder.WriteUInt16(state, 9, chargesRemaining); // bytes 9..10
    ItemStateEncoder.WriteUInt32(state, 11, unlockedAt);    // bytes 11..14
    // ... persist the state on the spawned item
}
```

Read it back later:

```csharp
var ownerSteamId = ItemStateEncoder.ReadUInt64(state, 0);
var isLocked     = ItemStateEncoder.ReadBool(state, 8);
var charges      = ItemStateEncoder.ReadUInt16(state, 9);
```

## Behaviour registry in 60 seconds

```csharp
using BlueBeard.Items;

public class MyPlugin : RocketPlugin
{
    public static ItemBehaviourManager Items { get; private set; }

    protected override void Load()
    {
        Items = new ItemBehaviourManager();

        Items.Register(1200, new MedkitBehaviour());
        Items.Register(3400, new LockpickBehaviour());

        Items.Load();
    }

    protected override void Unload()
    {
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

`OnEquipped` and `OnDequipped` fire automatically whenever the player switches their active useable.

## What doesn't fire automatically

Unturned does not expose a universal hook for item "use", "drop", or "pickup", so `ItemBehaviourManager` can't dispatch those on its own. Call the matching `NotifyUsed`, `NotifyDropped`, or `NotifyPickedUp` helper from wherever your plugin detects those events:

```csharp
// From a command, a useable override, a barricade interaction, etc:
MyPlugin.Items.NotifyUsed(player, itemJar);
```

## Quick Reference

| API | Purpose |
|-----|---------|
| `ItemStateEncoder.Write*` / `Read*` | Encode/decode primitives at arbitrary offsets |
| `ItemStateValidator.IsSafeForCustomState(asset)` | Returns false for weapons/attachments |
| `ItemBehaviourManager.Register(assetId, behaviour)` | Attach a handler to an asset ID |
| `ItemBehaviourManager.Unregister(assetId)` | Detach a handler |
| `ItemBehaviourManager.GetBehaviour(assetId)` | Lookup a registered handler (null if none) |
| `ItemBehaviourManager.NotifyUsed/Dropped/PickedUp` | Manual dispatch for events Unturned doesn't globally expose |

# BlueBeard.Items

Two complementary subsystems for item customisation in Unturned plugins: a low-level state encoder for writing custom data into item state byte arrays, and a high-level behaviour registry for attaching server-side logic to specific item asset IDs.

## Features

- `ItemStateEncoder` — little-endian read/write helpers for `ushort`, `uint`, `ulong`, `Guid`, `bool`, and length-prefixed UTF-8 strings
- `ItemStateValidator` — gatekeeper that rejects asset types where custom encoding would corrupt weapon / attachment state
- `IItemBehaviour` + `ItemBehaviourBase` — per-asset-id server hooks for equip, dequip, use, drop, pickup
- `ItemBehaviourManager` — dispatches equip/dequip automatically via `PlayerEquipment.OnUseableChanged_Global`; exposes `NotifyUsed`, `NotifyDropped`, and `NotifyPickedUp` for plugin-specific manual dispatch of the other events

## Quick example

```csharp
// 1. State encoding
if (ItemStateValidator.IsSafeForCustomState(myAssetId))
{
    var state = new byte[18];
    ItemStateEncoder.WriteUInt64(state, 0, ownerSteamId);
    ItemStateEncoder.WriteBool(state, 8, isLocked);
    ItemStateEncoder.WriteUInt16(state, 9, chargesRemaining);
}

// 2. Behaviour
public class MedkitBehaviour : ItemBehaviourBase
{
    public override void OnUsed(Player player, ItemJar jar)
    {
        // Custom heal logic
    }
}

itemBehaviours.Register(1200, new MedkitBehaviour());
itemBehaviours.Load();
```

See `docs/Items/` in the Infrastructure repo for full reference.

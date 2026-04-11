# BlueBeard.Items

BlueBeard.Items provides two complementary subsystems for item customisation in Unturned plugins: a low-level state encoder for writing custom data into item state byte arrays, and a high-level behaviour registry for attaching server-side logic to specific item asset IDs.

## Features

- **State encoding** -- Little-endian read/write helpers for `ushort`, `uint`, `ulong`, `Guid`, `bool`, and length-prefixed UTF-8 strings into arbitrary offsets in a `byte[]`.
- **Safety validator** -- Rejects weapon / attachment assets where custom encoding would corrupt Unturned's own state layout.
- **Behaviour interface** -- `IItemBehaviour` (and `ItemBehaviourBase` for virtual no-op defaults) exposes `OnEquipped`, `OnDequipped`, `OnUsed`, `OnDropped`, `OnPickedUp`.
- **Per-asset registry** -- `ItemBehaviourManager` maps asset IDs to handlers. Automatic equip/dequip dispatch via `PlayerEquipment.OnUseableChanged_Global`.
- **Manual-dispatch helpers** -- `NotifyUsed`, `NotifyDropped`, `NotifyPickedUp` for plugin-specific event wiring where Unturned does not expose a universal hook.
- **Independent subsystems** -- Use the encoder without the registry, or the registry without the encoder.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Installation, when to use which subsystem |
| [State Encoding](State-Encoding.md) | `ItemStateEncoder` API, layout discipline, the validator |
| [Behaviour Registry](Behaviour-Registry.md) | `ItemBehaviourManager` + `IItemBehaviour` + `ItemBehaviourBase` |
| [Examples](Examples.md) | Storage crate with owner ID, hotwire-able vehicle, reusable medkit |

## Source Classes

| Class / Interface | Role |
|-------------------|------|
| `ItemStateEncoder` | Static little-endian read/write helpers |
| `ItemStateValidator` | Rejects asset types where custom encoding is unsafe |
| `IItemBehaviour` | Interface for per-asset behaviour hooks |
| `ItemBehaviourBase` | Abstract base with virtual no-op implementations |
| `ItemBehaviourManager` | `IManager` that registers behaviours and routes item events |

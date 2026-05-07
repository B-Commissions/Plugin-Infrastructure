# BlueBeard.Items

BlueBeard.Items provides three complementary subsystems for entity customisation in Unturned plugins:

1. **State encoding** — read/write helpers for primitives in arbitrary byte arrays (item state, barricade state, etc.). Two flavours: an offset-based static API and a cursor-based wrapper that tracks position automatically.
2. **Safety validator** — refuses to encode custom data into asset types whose state Unturned itself interprets (weapons, attachments).
3. **Behaviour registries** — per-key handler dispatch for **items, barricades, structures, vehicles, zombies, and animals**. Subscribes to the relevant SDG events and routes them to your handler.

## Features

- **Cursor API** — `StateWriter` / `StateReader` track byte position so callers don't write offsets manually. Bit-identical to the static encoder; use whichever is more readable.
- **Static encoder** — `ItemStateEncoder` for explicit-offset use cases. Little-endian for `ushort`, `uint`, `ulong`, `Guid`, `bool`, length-prefixed UTF-8 strings.
- **Six behaviour managers** — items, barricades, structures, vehicles, zombies, animals — all sharing one registry shape inherited from `EntityBehaviourRegistry<TKey,TBehaviour>`.
- **Auto + manual dispatch** — each manager auto-wires the SDG events that exist as global hooks; the rest are exposed as `Notify*` helpers you call from your own code.
- **Composable** — registries are independent; use only what you need.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Installation, when to use which subsystem |
| [State Encoding](State-Encoding.md) | `ItemStateEncoder` static API + `StateWriter` / `StateReader` cursor API + the validator |
| [Behaviour Registry](Behaviour-Registry.md) | All six managers (items, barricades, structures, vehicles, zombies, animals), their interfaces, and what's auto vs manual |
| [Examples](Examples.md) | Storage crate, locked medkit, lockpick veto, VIP vehicle, salvage-protect crate |

## Source classes

### State encoding

| Class | Role |
|-------|------|
| `ItemStateEncoder` | Static little-endian read/write helpers (offset-based) |
| `StateWriter` | Cursor wrapper around `ItemStateEncoder` for sequential writes |
| `StateReader` | Cursor wrapper around `ItemStateEncoder` for sequential reads |
| `ItemStateValidator` | Refuses asset types where custom encoding is unsafe |

### Behaviour registries

| Class / Interface | Entity | Key |
|-------------------|--------|-----|
| `EntityBehaviourRegistry<TKey,TBehaviour>` | (generic base) | — |
| `IItemBehaviour` / `ItemBehaviourBase` / `ItemBehaviourManager` | Items | `ushort` asset id |
| `IBarricadeBehaviour` / `BarricadeBehaviourBase` / `BarricadeBehaviourManager` | Barricades | `ushort` asset id |
| `IStructureBehaviour` / `StructureBehaviourBase` / `StructureBehaviourManager` | Structures | `ushort` asset id |
| `IVehicleBehaviour` / `VehicleBehaviourBase` / `VehicleBehaviourManager` | Vehicles | `ushort` asset id |
| `IZombieBehaviour` / `ZombieBehaviourBase` / `ZombieBehaviourManager` | Zombies | `byte` zombie type |
| `IAnimalBehaviour` / `AnimalBehaviourBase` / `AnimalBehaviourManager` | Animals | `ushort` asset id |

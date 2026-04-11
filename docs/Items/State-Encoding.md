# State Encoding

`ItemStateEncoder` is a static helper class for reading and writing primitive values into an arbitrary byte offset in an item's state array. All operations are little-endian. The encoder is intentionally low-level -- callers are responsible for ensuring the byte array is large enough and offsets don't overlap.

## Layout discipline

Decide your layout up front and write it down. A comment in the code is usually enough:

```csharp
// Storage crate state layout (18 bytes):
//   0..7   ulong  ownerSteamId
//   8      bool   isLocked
//   9..10  ushort chargesRemaining
//   11..14 uint   unlockedAt (unix seconds)
//   15..17 reserved
```

Keep this comment adjacent to the writer / reader code. Changing the layout retroactively breaks every existing spawned item, so pick generous reserved ranges up front.

## Read / write reference

| Method | Bytes |
|--------|-------|
| `WriteUInt16 / ReadUInt16` | 2 |
| `WriteUInt32 / ReadUInt32` | 4 |
| `WriteUInt64 / ReadUInt64` | 8 |
| `WriteGuid / ReadGuid` | 16 |
| `WriteBool / ReadBool` | 1 |
| `WriteString(buf, offset, value, maxBytes)` / `ReadString(buf, offset)` | 2 + UTF-8 byte length |

### Strings

Strings are length-prefixed UTF-8: the first two bytes at `offset` are a little-endian `ushort` holding the encoded byte length, followed by that many UTF-8 bytes.

```csharp
ItemStateEncoder.WriteString(state, 20, "owner_name", maxBytes: 32);
//                                   ^^    ^^^^^^^^^^   ^^^^^^^^^^^^
//                                   offset  value       total capacity
```

`WriteString` throws `ArgumentException` if the encoded bytes don't fit in `maxBytes - 2`. Passing `null` writes an empty string (length prefix of 0).

`ReadString` returns `string.Empty` if the length prefix is 0.

### Endianness

Every integer helper is little-endian via explicit bit shifts (not `BitConverter`), so the layout is consistent across architectures. The explicit test `ItemStateEncoderTests.Little_Endian_Byte_Order_For_UInt32` verifies this.

## ItemStateValidator

Call `ItemStateValidator.IsSafeForCustomState` before encoding. It returns `false` for:

- `ItemGunAsset`
- `ItemMeleeAsset`
- `ItemThrowableAsset`
- `ItemMagazineAsset`
- `ItemSightAsset`
- `ItemTacticalAsset`
- `ItemGripAsset`
- `ItemBarrelAsset`

These types have fixed-layout state bytes that Unturned itself interprets for things like ammo count, fire mode, attachment slots, and durability. Writing custom data into those offsets will corrupt the item client-side, break attachments, or crash on save/load.

Two overloads are provided:

```csharp
// By asset instance:
if (ItemStateValidator.IsSafeForCustomState(asset)) { /* ok */ }

// By asset id (looks up the asset via Assets.find):
if (ItemStateValidator.IsSafeForCustomState(assetId)) { /* ok */ }
```

If you really need to encode data on a gun asset (for cosmetic skins, owner tagging, etc.), the safe path is to pick a custom asset ID via an `ItemAsset` replacement -- not to fight the encoder's safety check.

## Sizing the state array

New items spawned with a custom state need an array large enough for your layout. The simplest approach is to allocate exactly what you need:

```csharp
const int StateSize = 18;
var state = new byte[StateSize];
// ... write ...
ItemManager.dropItem(new Item(assetId, 1, 100, state), position, false, true, true);
```

If you're mutating an existing item, the state array might already be allocated with a different size. Resize it if your layout doesn't fit:

```csharp
if (item.state == null || item.state.Length < StateSize)
    item.state = new byte[StateSize];
ItemStateEncoder.WriteUInt64(item.state, 0, ownerSteamId);
```

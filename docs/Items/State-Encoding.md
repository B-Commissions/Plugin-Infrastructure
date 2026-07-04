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

## Cursor API: StateWriter / StateReader

If you don't want to track offsets manually, use the cursor-style wrappers `StateWriter` and `StateReader`. They hold the buffer plus a position; each `Write*` / `Read*` advances the cursor by the size of the value. Output bytes are identical to the static encoder -- the cursor delegates to it -- so the two styles are fully interchangeable.

```csharp
// Write
jar.item.state = new StateWriter(13)
    .WriteUInt32(charges)
    .WriteUInt64(unlockUnix)
    .WriteBool(locked)
    .ToArray();

// Read
var r = new StateReader(jar.item.state);
var charges    = r.ReadUInt32();
var unlockUnix = r.ReadUInt64();
var locked     = r.ReadBool();
```

Constructors:

| Constructor | Use when |
|-------------|----------|
| `new StateWriter(int size)` | Allocating a new buffer for a freshly spawned item |
| `new StateWriter(byte[] buf, int startOffset = 0)` | Mutating an existing item's state in place |
| `new StateReader(byte[] buf, int startOffset = 0)` | Reading from an existing item's state |

`Seek(offset)` repositions the cursor; `Skip(count)` advances without writing/reading (useful for reserved regions). `Position`, `Length`, `Remaining`, and `Buffer` are exposed for diagnostics.

`WriteString(value, maxBytes)` / `ReadString(maxBytes)` reserve a fixed `maxBytes` slot regardless of actual encoded length, so subsequent fields land at predictable offsets. Pass the same `maxBytes` to both calls.

The static `ItemStateEncoder` API remains supported for explicit-offset use cases. Pick whichever style fits the call site -- they produce the same bytes.

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

## Tagged block headers (versioned custom state)

For custom state that must evolve after plugins ship, prefix your payload with a 4-byte
block header (magic + version) instead of relying on fixed offsets:

```csharp
const ushort MyMagic = 0x4B57;

// write
var w = new StateWriter(state, offset);
w.WriteBlockHeader(MyMagic, version: 2)
 .WriteUInt32(charges)
 .WriteBool(favourite);          // added in v2

// read
var r = new StateReader(state, offset);
if (r.TryReadBlockHeader(MyMagic, out var version))
{
    var charges = r.ReadUInt32();
    var favourite = version >= 2 && r.ReadBool();
}
```

`TryReadBlockHeader` restores the cursor and returns false on a magic mismatch, so probing
state written before the block convention is safe. The header is plain UInt16 writes — the
static `ItemStateEncoder` API remains byte-compatible and untouched.

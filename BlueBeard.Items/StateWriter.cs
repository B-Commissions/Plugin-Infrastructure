using System;

namespace BlueBeard.Items;

/// <summary>
/// Cursor-style wrapper around <see cref="ItemStateEncoder"/>: maintains the byte position
/// for you, so callers can write a sequence of values without tracking offsets manually.
/// Each Write* call advances the cursor by the size of the value just written.
///
/// Bytes produced are identical to calling <see cref="ItemStateEncoder"/> directly with the
/// equivalent offsets — the cursor delegates to the static methods. Mix and match freely.
/// </summary>
public sealed class StateWriter
{
    private readonly byte[] _state;
    private int _pos;

    public StateWriter(byte[] state, int startOffset = 0)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        if (startOffset < 0 || startOffset > state.Length)
            throw new ArgumentOutOfRangeException(nameof(startOffset));
        _pos = startOffset;
    }

    public StateWriter(int size) : this(new byte[size]) { }

    public int Position => _pos;
    public int Length => _state.Length;
    public int Remaining => _state.Length - _pos;
    public byte[] Buffer => _state;

    public StateWriter Seek(int offset)
    {
        if (offset < 0 || offset > _state.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        _pos = offset;
        return this;
    }

    public StateWriter Skip(int count)
    {
        if (count < 0 || _pos + count > _state.Length)
            throw new ArgumentOutOfRangeException(nameof(count));
        _pos += count;
        return this;
    }

    public StateWriter WriteUInt16(ushort value)
    {
        ItemStateEncoder.WriteUInt16(_state, _pos, value);
        _pos += 2;
        return this;
    }

    public StateWriter WriteUInt32(uint value)
    {
        ItemStateEncoder.WriteUInt32(_state, _pos, value);
        _pos += 4;
        return this;
    }

    public StateWriter WriteUInt64(ulong value)
    {
        ItemStateEncoder.WriteUInt64(_state, _pos, value);
        _pos += 8;
        return this;
    }

    public StateWriter WriteBool(bool value)
    {
        ItemStateEncoder.WriteBool(_state, _pos, value);
        _pos += 1;
        return this;
    }

    public StateWriter WriteGuid(Guid value)
    {
        ItemStateEncoder.WriteGuid(_state, _pos, value);
        _pos += 16;
        return this;
    }

    /// <summary>
    /// Writes a length-prefixed UTF-8 string, reserving exactly <paramref name="maxBytes"/>
    /// bytes regardless of actual encoded length. Cursor advances by <paramref name="maxBytes"/>
    /// so subsequent writes land at a predictable offset.
    /// </summary>
    public StateWriter WriteString(string value, int maxBytes)
    {
        ItemStateEncoder.WriteString(_state, _pos, value, maxBytes);
        _pos += maxBytes;
        return this;
    }

    /// <summary>
    /// Write a 4-byte tagged-block header (magic + version) in front of custom state so
    /// shipped plugins can evolve their layout without offset math: bump the version when
    /// the payload changes, and readers branch via
    /// <see cref="StateReader.TryReadBlockHeader"/>. Plain UInt16 writes underneath —
    /// fully compatible with the static encoder.
    /// </summary>
    public StateWriter WriteBlockHeader(ushort magic, ushort version) =>
        WriteUInt16(magic).WriteUInt16(version);

    public byte[] ToArray() => _state;
}

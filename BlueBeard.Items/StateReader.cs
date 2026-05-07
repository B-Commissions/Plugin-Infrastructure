using System;

namespace BlueBeard.Items;

/// <summary>
/// Cursor-style wrapper around <see cref="ItemStateEncoder"/> for reading: maintains the
/// byte position so callers can read a sequence of values in the same order they were
/// written without tracking offsets. Each Read* call advances the cursor.
/// </summary>
public sealed class StateReader
{
    private readonly byte[] _state;
    private int _pos;

    public StateReader(byte[] state, int startOffset = 0)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        if (startOffset < 0 || startOffset > state.Length)
            throw new ArgumentOutOfRangeException(nameof(startOffset));
        _pos = startOffset;
    }

    public int Position => _pos;
    public int Length => _state.Length;
    public int Remaining => _state.Length - _pos;
    public byte[] Buffer => _state;

    public StateReader Seek(int offset)
    {
        if (offset < 0 || offset > _state.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        _pos = offset;
        return this;
    }

    public StateReader Skip(int count)
    {
        if (count < 0 || _pos + count > _state.Length)
            throw new ArgumentOutOfRangeException(nameof(count));
        _pos += count;
        return this;
    }

    public ushort ReadUInt16()
    {
        var value = ItemStateEncoder.ReadUInt16(_state, _pos);
        _pos += 2;
        return value;
    }

    public uint ReadUInt32()
    {
        var value = ItemStateEncoder.ReadUInt32(_state, _pos);
        _pos += 4;
        return value;
    }

    public ulong ReadUInt64()
    {
        var value = ItemStateEncoder.ReadUInt64(_state, _pos);
        _pos += 8;
        return value;
    }

    public bool ReadBool()
    {
        var value = ItemStateEncoder.ReadBool(_state, _pos);
        _pos += 1;
        return value;
    }

    public Guid ReadGuid()
    {
        var value = ItemStateEncoder.ReadGuid(_state, _pos);
        _pos += 16;
        return value;
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 string written with
    /// <see cref="StateWriter.WriteString(string,int)"/>. Pass the same <paramref name="maxBytes"/>
    /// used at write time so the cursor advances by the same fixed-size region.
    /// </summary>
    public string ReadString(int maxBytes)
    {
        var value = ItemStateEncoder.ReadString(_state, _pos);
        _pos += maxBytes;
        return value;
    }
}

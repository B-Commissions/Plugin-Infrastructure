using System;
using System.Text;

namespace BlueBeard.Items;

/// <summary>
/// Low-level helpers for reading and writing primitive values into the item state byte array
/// at arbitrary offsets. Everything is little-endian. Callers are responsible for ensuring
/// the state array is large enough and that offsets do not overlap each other.
///
/// Do not call these on weapon / attachment state (gun, melee, throwable, magazine, sight,
/// tactical, grip, barrel) — Unturned interprets specific byte offsets on those items and
/// custom encoding will corrupt them. Use <see cref="ItemStateValidator.IsSafeForCustomState(ushort)"/>
/// to check before encoding.
/// </summary>
public static class ItemStateEncoder
{
    // -------- UInt16 --------

    public static void WriteUInt16(byte[] state, int offset, ushort value)
    {
        state[offset]     = (byte)(value & 0xFF);
        state[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    public static ushort ReadUInt16(byte[] state, int offset)
    {
        return (ushort)(state[offset] | (state[offset + 1] << 8));
    }

    // -------- UInt32 --------

    public static void WriteUInt32(byte[] state, int offset, uint value)
    {
        state[offset]     = (byte)(value & 0xFF);
        state[offset + 1] = (byte)((value >> 8) & 0xFF);
        state[offset + 2] = (byte)((value >> 16) & 0xFF);
        state[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    public static uint ReadUInt32(byte[] state, int offset)
    {
        return (uint)(state[offset]
            | (state[offset + 1] << 8)
            | (state[offset + 2] << 16)
            | (state[offset + 3] << 24));
    }

    // -------- UInt64 --------

    public static void WriteUInt64(byte[] state, int offset, ulong value)
    {
        for (var i = 0; i < 8; i++)
            state[offset + i] = (byte)((value >> (8 * i)) & 0xFF);
    }

    public static ulong ReadUInt64(byte[] state, int offset)
    {
        ulong value = 0;
        for (var i = 0; i < 8; i++)
            value |= (ulong)state[offset + i] << (8 * i);
        return value;
    }

    // -------- Guid (16 bytes) --------

    public static void WriteGuid(byte[] state, int offset, Guid value)
    {
        var bytes = value.ToByteArray();
        Buffer.BlockCopy(bytes, 0, state, offset, 16);
    }

    public static Guid ReadGuid(byte[] state, int offset)
    {
        var bytes = new byte[16];
        Buffer.BlockCopy(state, offset, bytes, 0, 16);
        return new Guid(bytes);
    }

    // -------- Bool (1 byte) --------

    public static void WriteBool(byte[] state, int offset, bool value)
    {
        state[offset] = value ? (byte)1 : (byte)0;
    }

    public static bool ReadBool(byte[] state, int offset)
    {
        return state[offset] != 0;
    }

    // -------- String (length-prefixed UTF-8) --------

    /// <summary>
    /// Writes a UTF-8 length-prefixed string. First 2 bytes are the little-endian byte length,
    /// then the UTF-8 bytes. The total bytes written (2 + byteLength) must not exceed
    /// <paramref name="maxBytes"/>. Passing null writes an empty string.
    /// </summary>
    public static void WriteString(byte[] state, int offset, string value, int maxBytes)
    {
        if (maxBytes < 2)
            throw new ArgumentException("maxBytes must be at least 2 to hold the length prefix.", nameof(maxBytes));

        var bytes = value == null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(value);
        if (bytes.Length > maxBytes - 2)
            throw new ArgumentException($"Encoded string is {bytes.Length} bytes but only {maxBytes - 2} are available.", nameof(value));
        if (bytes.Length > ushort.MaxValue)
            throw new ArgumentException("Encoded string is longer than ushort.MaxValue.", nameof(value));

        WriteUInt16(state, offset, (ushort)bytes.Length);
        if (bytes.Length > 0)
            Buffer.BlockCopy(bytes, 0, state, offset + 2, bytes.Length);
    }

    public static string ReadString(byte[] state, int offset)
    {
        var length = ReadUInt16(state, offset);
        if (length == 0) return string.Empty;
        return Encoding.UTF8.GetString(state, offset + 2, length);
    }
}

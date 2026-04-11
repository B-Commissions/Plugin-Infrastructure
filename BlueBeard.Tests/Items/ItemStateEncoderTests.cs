using System;
using BlueBeard.Items;
using Xunit;

namespace BlueBeard.Tests.Items;

public class ItemStateEncoderTests
{
    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)12345)]
    [InlineData(ushort.MaxValue)]
    public void UInt16_RoundTrip(ushort value)
    {
        var buf = new byte[2];
        ItemStateEncoder.WriteUInt16(buf, 0, value);
        Assert.Equal(value, ItemStateEncoder.ReadUInt16(buf, 0));
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(1234567890u)]
    [InlineData(uint.MaxValue)]
    public void UInt32_RoundTrip(uint value)
    {
        var buf = new byte[4];
        ItemStateEncoder.WriteUInt32(buf, 0, value);
        Assert.Equal(value, ItemStateEncoder.ReadUInt32(buf, 0));
    }

    [Theory]
    [InlineData(0ul)]
    [InlineData(1ul)]
    [InlineData(1234567890123456789ul)]
    [InlineData(ulong.MaxValue)]
    public void UInt64_RoundTrip(ulong value)
    {
        var buf = new byte[8];
        ItemStateEncoder.WriteUInt64(buf, 0, value);
        Assert.Equal(value, ItemStateEncoder.ReadUInt64(buf, 0));
    }

    [Fact]
    public void Guid_RoundTrip()
    {
        var guid = Guid.NewGuid();
        var buf = new byte[16];
        ItemStateEncoder.WriteGuid(buf, 0, guid);
        Assert.Equal(guid, ItemStateEncoder.ReadGuid(buf, 0));
    }

    [Fact]
    public void Empty_Guid_RoundTrip()
    {
        var buf = new byte[16];
        ItemStateEncoder.WriteGuid(buf, 0, Guid.Empty);
        Assert.Equal(Guid.Empty, ItemStateEncoder.ReadGuid(buf, 0));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Bool_RoundTrip(bool value)
    {
        var buf = new byte[1];
        ItemStateEncoder.WriteBool(buf, 0, value);
        Assert.Equal(value, ItemStateEncoder.ReadBool(buf, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("with spaces and symbols !@#$%^&*()")]
    [InlineData("unicode: 你好 мир 🎉")]
    public void String_RoundTrip(string value)
    {
        var buf = new byte[128];
        ItemStateEncoder.WriteString(buf, 0, value, buf.Length);
        Assert.Equal(value, ItemStateEncoder.ReadString(buf, 0));
    }

    [Fact]
    public void String_Null_Writes_As_Empty()
    {
        var buf = new byte[16];
        ItemStateEncoder.WriteString(buf, 0, null, buf.Length);
        Assert.Equal(string.Empty, ItemStateEncoder.ReadString(buf, 0));
    }

    [Fact]
    public void String_Exceeding_MaxBytes_Throws()
    {
        var buf = new byte[6];
        Assert.Throws<ArgumentException>(() => ItemStateEncoder.WriteString(buf, 0, "too long", 6));
    }

    [Fact]
    public void Multiple_Values_At_Different_Offsets_Do_Not_Interfere()
    {
        var buf = new byte[32];
        ItemStateEncoder.WriteUInt64(buf, 0, 0xDEADBEEFCAFEBABEul);
        ItemStateEncoder.WriteBool(buf, 8, true);
        ItemStateEncoder.WriteUInt16(buf, 9, 42);
        ItemStateEncoder.WriteUInt32(buf, 11, 1_000_000u);

        Assert.Equal(0xDEADBEEFCAFEBABEul, ItemStateEncoder.ReadUInt64(buf, 0));
        Assert.True(ItemStateEncoder.ReadBool(buf, 8));
        Assert.Equal((ushort)42, ItemStateEncoder.ReadUInt16(buf, 9));
        Assert.Equal(1_000_000u, ItemStateEncoder.ReadUInt32(buf, 11));
    }

    [Fact]
    public void Little_Endian_Byte_Order_For_UInt32()
    {
        var buf = new byte[4];
        ItemStateEncoder.WriteUInt32(buf, 0, 0x11223344u);
        Assert.Equal(0x44, buf[0]);
        Assert.Equal(0x33, buf[1]);
        Assert.Equal(0x22, buf[2]);
        Assert.Equal(0x11, buf[3]);
    }
}

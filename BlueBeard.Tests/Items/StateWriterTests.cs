using System;
using BlueBeard.Items;
using Xunit;

namespace BlueBeard.Tests.Items;

public class StateWriterTests
{
    [Fact]
    public void Cursor_Advances_By_Type_Size()
    {
        var w = new StateWriter(32);
        Assert.Equal(0, w.Position);
        w.WriteUInt64(1ul);    Assert.Equal(8, w.Position);
        w.WriteBool(true);     Assert.Equal(9, w.Position);
        w.WriteUInt16(42);     Assert.Equal(11, w.Position);
        w.WriteUInt32(7u);     Assert.Equal(15, w.Position);
        w.WriteGuid(Guid.NewGuid()); Assert.Equal(31, w.Position);
        w.WriteBool(false);    Assert.Equal(32, w.Position);
    }

    [Fact]
    public void Sequential_Writes_Match_Static_Encoder_Bytes()
    {
        var direct = new byte[32];
        ItemStateEncoder.WriteUInt64(direct, 0, 0xDEADBEEFCAFEBABEul);
        ItemStateEncoder.WriteBool(direct, 8, true);
        ItemStateEncoder.WriteUInt16(direct, 9, 42);
        ItemStateEncoder.WriteUInt32(direct, 11, 1_000_000u);

        var via = new StateWriter(32)
            .WriteUInt64(0xDEADBEEFCAFEBABEul)
            .WriteBool(true)
            .WriteUInt16(42)
            .WriteUInt32(1_000_000u)
            .ToArray();

        Assert.Equal(direct, via);
    }

    [Fact]
    public void Reader_Recovers_Values_In_Order()
    {
        var buf = new StateWriter(32)
            .WriteUInt64(0xDEADBEEFCAFEBABEul)
            .WriteBool(true)
            .WriteUInt16(42)
            .WriteUInt32(1_000_000u)
            .ToArray();

        var r = new StateReader(buf);
        Assert.Equal(0xDEADBEEFCAFEBABEul, r.ReadUInt64());
        Assert.True(r.ReadBool());
        Assert.Equal((ushort)42, r.ReadUInt16());
        Assert.Equal(1_000_000u, r.ReadUInt32());
    }

    [Fact]
    public void Wrap_Existing_Buffer_Preserves_Content()
    {
        var buf = new byte[16];
        ItemStateEncoder.WriteUInt64(buf, 0, 0x1122334455667788ul);

        var r = new StateReader(buf);
        Assert.Equal(0x1122334455667788ul, r.ReadUInt64());
    }

    [Fact]
    public void Start_Offset_Skips_Bytes()
    {
        var buf = new byte[16];
        ItemStateEncoder.WriteUInt32(buf, 4, 0xCAFEBABEu);

        var r = new StateReader(buf, startOffset: 4);
        Assert.Equal(0xCAFEBABEu, r.ReadUInt32());
    }

    [Fact]
    public void String_Round_Trip_Through_Cursor()
    {
        const int field = 32;
        var buf = new StateWriter(field + 8)
            .WriteString("hello world", field)
            .WriteUInt64(99ul)
            .ToArray();

        var r = new StateReader(buf);
        Assert.Equal("hello world", r.ReadString(field));
        Assert.Equal(99ul, r.ReadUInt64());
    }

    [Fact]
    public void Seek_And_Skip_Reposition_Cursor()
    {
        var w = new StateWriter(16);
        w.Skip(4).WriteUInt32(0xCAFEBABEu);
        Assert.Equal(0xCAFEBABEu, ItemStateEncoder.ReadUInt32(w.Buffer, 4));

        w.Seek(0).WriteUInt32(0x11223344u);
        Assert.Equal(0x11223344u, ItemStateEncoder.ReadUInt32(w.Buffer, 0));
    }

    [Fact]
    public void Constructor_Allocates_Buffer_Of_Given_Size()
    {
        var w = new StateWriter(13);
        Assert.Equal(13, w.Length);
        Assert.Equal(13, w.Remaining);
        Assert.Equal(0, w.Position);
    }

    [Fact]
    public void Null_Buffer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new StateWriter((byte[])null));
        Assert.Throws<ArgumentNullException>(() => new StateReader(null));
    }

    [Fact]
    public void Negative_Or_Out_Of_Range_StartOffset_Throws()
    {
        var buf = new byte[8];
        Assert.Throws<ArgumentOutOfRangeException>(() => new StateWriter(buf, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StateWriter(buf, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StateReader(buf, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StateReader(buf, 9));
    }

    [Fact]
    public void Skip_Past_End_Throws()
    {
        var w = new StateWriter(8);
        w.Skip(8);
        Assert.Throws<ArgumentOutOfRangeException>(() => w.Skip(1));
    }
}

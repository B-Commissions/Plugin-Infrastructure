using System;
using BlueBeard.Items.Behaviours;
using Xunit;

namespace BlueBeard.Tests.Items;

public class EntityBehaviourRegistryTests
{
    private interface IFakeBehaviour { }
    private sealed class FakeBehaviour : IFakeBehaviour { public string Name = ""; }

    private sealed class FakeRegistry : EntityBehaviourRegistry<ushort, IFakeBehaviour>
    {
        public override void Load() { }
    }

    [Fact]
    public void Register_Then_Get_Returns_Same_Instance()
    {
        var r = new FakeRegistry();
        var b = new FakeBehaviour { Name = "first" };
        r.Register(123, b);
        Assert.Same(b, r.GetBehaviour(123));
    }

    [Fact]
    public void GetBehaviour_Missing_Returns_Null()
    {
        var r = new FakeRegistry();
        Assert.Null(r.GetBehaviour(999));
    }

    [Fact]
    public void Register_Overwrites_Previous()
    {
        var r = new FakeRegistry();
        r.Register(7, new FakeBehaviour { Name = "first" });
        r.Register(7, new FakeBehaviour { Name = "second" });
        var got = (FakeBehaviour)r.GetBehaviour(7);
        Assert.Equal("second", got.Name);
    }

    [Fact]
    public void Unregister_Removes_Entry()
    {
        var r = new FakeRegistry();
        r.Register(42, new FakeBehaviour());
        r.Unregister(42);
        Assert.Null(r.GetBehaviour(42));
    }

    [Fact]
    public void Register_Null_Behaviour_Throws()
    {
        var r = new FakeRegistry();
        Assert.Throws<ArgumentNullException>(() => r.Register(1, null));
    }

    [Fact]
    public void Unload_Clears_All()
    {
        var r = new FakeRegistry();
        r.Register(1, new FakeBehaviour());
        r.Register(2, new FakeBehaviour());
        r.Unload();
        Assert.Empty(r.All);
    }

    [Fact]
    public void TryGet_Reflects_Registration_State()
    {
        var r = new FakeRegistry();
        Assert.False(r.TryGet(5, out _));
        var b = new FakeBehaviour();
        r.Register(5, b);
        Assert.True(r.TryGet(5, out var got));
        Assert.Same(b, got);
    }
}

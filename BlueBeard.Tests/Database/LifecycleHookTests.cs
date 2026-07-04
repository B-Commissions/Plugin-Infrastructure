using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueBeard.Database;
using BlueBeard.Database.Attributes;
using Xunit;

namespace BlueBeard.Tests.Database;

// ---------------------------------------------------------------------------
// Entities
// ---------------------------------------------------------------------------

[Table("hooked")]
public class HookedEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Column("balance")] public int Balance { get; set; }
    [Column("player_name")] public string PlayerName { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<string> Calls { get; } = [];

    [BeforeInsert]
    private void Stamp()
    {
        UpdatedAt = new DateTime(2026, 1, 1);
        Calls.Add("BeforeInsert");
    }

    [AfterInsert]
    private Task AfterInsertAsync()
    {
        Calls.Add("AfterInsert");
        return Task.CompletedTask;
    }

    [BeforeUpdate("balance")]
    private void OnBalance(int value) => Calls.Add($"BeforeUpdate:balance={value}");

    // Targets by property name (fallback path) with the column's real CLR type.
    [AfterUpdate("PlayerName")]
    private void OnNamed(string value) => Calls.Add($"AfterUpdate:name={value}");

    // One method carrying two hook attributes for different stages.
    [BeforeDelete]
    [AfterDelete]
    private void OnDelete() => Calls.Add("Delete");
}

[Table("hooked_nullable")]
public class NullableHookEntity
{
    [PrimaryKey] public int Id { get; set; }
    [Column("score")] public int? Score { get; set; }

    public List<string> Calls { get; } = [];

    // Non-nullable parameter for a nullable column: skipped when the value is null.
    [BeforeUpdate("score")]
    private void OnScore(int value) => Calls.Add($"score={value}");
}

public class BadReturnEntity
{
    [PrimaryKey] public int Id { get; set; }
    [BeforeInsert] private int Bad() => 0;
}

public class BadParamCountEntity
{
    [PrimaryKey] public int Id { get; set; }
    [BeforeInsert] private void Bad(int x) { }
}

public class BadColumnEntity
{
    [PrimaryKey] public int Id { get; set; }
    [BeforeUpdate("nonexistent")] private void Bad(int value) { }
}

public class BadTypeEntity
{
    [PrimaryKey] public int Id { get; set; }
    [Column("name")] public string Name { get; set; }
    [BeforeUpdate("name")] private void Bad(int value) { }
}

// ---------------------------------------------------------------------------
// Discovery + validation
// ---------------------------------------------------------------------------

public class HookDiscoveryTests
{
    [Fact]
    public void Discovers_All_Hooks_With_Kinds_And_Targets()
    {
        var meta = TableMetadata.For<HookedEntity>();

        Assert.Equal(6, meta.Hooks.Count);
        Assert.Single(meta.Hooks, h => h.Kind == HookKind.BeforeInsert && h.TargetColumn == null);
        Assert.Single(meta.Hooks, h => h.Kind == HookKind.AfterInsert && h.TargetColumn == null);
        Assert.Single(meta.Hooks, h => h.Kind == HookKind.BeforeUpdate && h.TargetColumn?.ColumnName == "balance");
        Assert.Single(meta.Hooks, h => h.Kind == HookKind.AfterUpdate && h.TargetColumn?.ColumnName == "player_name");
        Assert.Single(meta.Hooks, h => h.Kind == HookKind.BeforeDelete);
        Assert.Single(meta.Hooks, h => h.Kind == HookKind.AfterDelete);
    }

    [Fact]
    public void Invalid_Return_Type_Throws_At_Metadata_Build()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TableMetadata.For<BadReturnEntity>());
        Assert.Contains("must return void or Task", ex.Message);
    }

    [Fact]
    public void Entity_Level_Hook_With_Parameters_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TableMetadata.For<BadParamCountEntity>());
        Assert.Contains("entity-level hooks must be parameterless", ex.Message);
    }

    [Fact]
    public void Unknown_Column_Target_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TableMetadata.For<BadColumnEntity>());
        Assert.Contains("'nonexistent' is not a mapped column", ex.Message);
    }

    [Fact]
    public void Parameter_Type_Mismatch_Throws_With_Precise_Message()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TableMetadata.For<BadTypeEntity>());
        Assert.Contains("parameter type Int32 does not match column 'name' of type String", ex.Message);
    }
}

// ---------------------------------------------------------------------------
// Dispatch
// ---------------------------------------------------------------------------

public class HookDispatchTests
{
    [Fact]
    public async Task Entity_Hooks_Fire_And_Mutations_Are_Visible()
    {
        var meta = TableMetadata.For<HookedEntity>();
        var entity = new HookedEntity();

        await HookRunner.RunAsync(meta, HookKind.BeforeInsert, entity);

        Assert.Equal(new DateTime(2026, 1, 1), entity.UpdatedAt);
        Assert.Equal(["BeforeInsert"], entity.Calls);
    }

    [Fact]
    public async Task Async_Hook_Is_Awaited()
    {
        var meta = TableMetadata.For<HookedEntity>();
        var entity = new HookedEntity();

        await HookRunner.RunAsync(meta, HookKind.AfterInsert, entity);

        Assert.Equal(["AfterInsert"], entity.Calls);
    }

    [Fact]
    public async Task Column_Hook_Receives_Typed_Current_Value()
    {
        var meta = TableMetadata.For<HookedEntity>();
        var entity = new HookedEntity { Balance = 42, PlayerName = "jack" };

        await HookRunner.RunAsync(meta, HookKind.BeforeUpdate, entity);
        await HookRunner.RunAsync(meta, HookKind.AfterUpdate, entity);

        Assert.Equal(["BeforeUpdate:balance=42", "AfterUpdate:name=jack"], entity.Calls);
    }

    [Fact]
    public async Task Multi_Attribute_Method_Fires_Per_Stage()
    {
        var meta = TableMetadata.For<HookedEntity>();
        var entity = new HookedEntity();

        await HookRunner.RunAsync(meta, HookKind.BeforeDelete, entity);
        await HookRunner.RunAsync(meta, HookKind.AfterDelete, entity);

        Assert.Equal(["Delete", "Delete"], entity.Calls);
    }

    [Fact]
    public async Task Null_Value_Skips_NonNullable_Parameter_Hook()
    {
        var meta = TableMetadata.For<NullableHookEntity>();

        var withValue = new NullableHookEntity { Score = 7 };
        await HookRunner.RunAsync(meta, HookKind.BeforeUpdate, withValue);
        Assert.Equal(["score=7"], withValue.Calls);

        var withNull = new NullableHookEntity { Score = null };
        await HookRunner.RunAsync(meta, HookKind.BeforeUpdate, withNull);
        Assert.Empty(withNull.Calls);
    }

    [Fact]
    public async Task Irrelevant_Stage_Fires_Nothing()
    {
        var meta = TableMetadata.For<HookedEntity>();
        var entity = new HookedEntity();

        await HookRunner.RunAsync(meta, HookKind.BeforeUpdate, entity);
        Assert.DoesNotContain("BeforeInsert", entity.Calls);
    }
}

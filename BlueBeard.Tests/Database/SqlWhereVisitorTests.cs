using System;
using System.Collections.Generic;
using System.Linq;
using BlueBeard.Database;
using BlueBeard.Database.Attributes;
using Xunit;

namespace BlueBeard.Tests.Database;

[Table("visitor_entity")]
public class VisitorEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Column("player_name")] public string Name { get; set; }
    public int Score { get; set; }
    public Guid Token { get; set; }
}

public class SqlWhereVisitorTests
{
    [Fact]
    public void Contains_Translates_To_Like_With_Wildcards()
    {
        var (sql, parameters) = SqlWhereVisitor.Translate<VisitorEntity>(e => e.Name.Contains("jack"));
        Assert.Equal("`player_name` LIKE @p0", sql);
        Assert.Equal("%jack%", parameters[0]);
    }

    [Fact]
    public void StartsWith_And_EndsWith_Anchor_Correctly()
    {
        var (sql1, p1) = SqlWhereVisitor.Translate<VisitorEntity>(e => e.Name.StartsWith("ja"));
        Assert.Equal("`player_name` LIKE @p0", sql1);
        Assert.Equal("ja%", p1[0]);

        var (_, p2) = SqlWhereVisitor.Translate<VisitorEntity>(e => e.Name.EndsWith("ck"));
        Assert.Equal("%ck", p2[0]);
    }

    [Fact]
    public void Like_Wildcards_In_Value_Are_Escaped()
    {
        var (_, parameters) = SqlWhereVisitor.Translate<VisitorEntity>(e => e.Name.Contains("100%_a\\b"));
        Assert.Equal("%100\\%\\_a\\\\b%", parameters[0]);
    }

    [Fact]
    public void IsNullOrEmpty_Expands_To_Null_Or_Empty_Check()
    {
        var (sql, parameters) = SqlWhereVisitor.Translate<VisitorEntity>(e => string.IsNullOrEmpty(e.Name));
        Assert.Equal("(`player_name` IS NULL OR `player_name` = '')", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void List_Contains_Translates_To_In()
    {
        var ids = new List<int> { 1, 2, 3 };
        var (sql, parameters) = SqlWhereVisitor.Translate<VisitorEntity>(e => ids.Contains(e.Id));
        Assert.Equal("`Id` IN (@p0, @p1, @p2)", sql);
        Assert.Equal([1, 2, 3], parameters.Cast<int>());
    }

    [Fact]
    public void Array_Contains_Translates_To_In()
    {
        var ids = new[] { 5, 6 };
        var (sql, parameters) = SqlWhereVisitor.Translate<VisitorEntity>(e => ids.Contains(e.Id));
        Assert.Equal("`Id` IN (@p0, @p1)", sql);
        Assert.Equal(2, parameters.Count);
    }

    [Fact]
    public void Empty_Collection_Contains_Matches_Nothing()
    {
        var ids = new List<int>();
        var (sql, parameters) = SqlWhereVisitor.Translate<VisitorEntity>(e => ids.Contains(e.Id));
        Assert.Equal("(1 = 0)", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void In_Values_Route_Through_Column_Converter()
    {
        var guid = Guid.NewGuid();
        var tokens = new List<Guid> { guid };
        var (sql, parameters) = SqlWhereVisitor.Translate<VisitorEntity>(e => tokens.Contains(e.Token));
        Assert.Equal("`Token` IN (@p0)", sql);
        Assert.Equal(guid.ToString(), parameters[0]); // GuidConverter stores CHAR(36)
    }

    [Fact]
    public void Method_Calls_On_Captured_Values_Still_Evaluate()
    {
        var (sql, parameters) = SqlWhereVisitor.Translate<VisitorEntity>(e => e.Name == "JACK".ToLowerInvariant());
        Assert.Equal("(`player_name` = @p0)", sql);
        Assert.Equal("jack", parameters[0]);
    }

    [Fact]
    public void Like_Combines_With_Other_Predicates()
    {
        var (sql, parameters) = SqlWhereVisitor.Translate<VisitorEntity>(
            e => e.Name.Contains("a") && e.Score > 10);
        Assert.Equal("(`player_name` LIKE @p0 AND (`Score` > @p1))", sql);
        Assert.Equal(2, parameters.Count);
    }

    [Fact]
    public void Untranslatable_Entity_Method_Throws_Clear_Error()
    {
        var ex = Assert.Throws<NotSupportedException>(() =>
            SqlWhereVisitor.Translate<VisitorEntity>(e => e.Name.Trim() == "x"));
        Assert.Contains("cannot be translated to SQL", ex.Message);
    }
}

public class TypeTokenNavigationTests
{
    [Table("tt_parent")]
    public class TtParent
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [HasMany] public List<TtChild> Children { get; set; }
    }

    [Table("tt_child")]
    public class TtChild
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [ForeignKey(typeof(TtParent))] public int ParentId { get; set; }
        [BelongsTo] public TtParent Parent { get; set; }
    }

    [Fact]
    public void Parameterless_HasMany_And_BelongsTo_Build_Metadata()
    {
        var parent = TableMetadata.For<TtParent>();
        var child = TableMetadata.For<TtChild>();

        var hasMany = Assert.Single(parent.Navigations);
        Assert.Null(hasMany.ForeignKeyProperty);
        Assert.Equal(typeof(TtChild), hasMany.ElementType);

        var belongsTo = Assert.Single(child.Navigations);
        Assert.Null(belongsTo.LocalKeyProperty);

        // The FK column resolves purely by type token.
        Assert.Equal("ParentId", child.GetForeignKeyColumnTo(typeof(TtParent)).PropertyName);
    }

    [Fact]
    public void ForeignKey_TypeOnly_Ctor_Falls_Back_To_Referenced_Pk_In_Ddl()
    {
        var sql = SchemaSync.GenerateCreateTable(TableMetadata.For<TtChild>());
        Assert.Contains("FOREIGN KEY (`ParentId`) REFERENCES `tt_parent`(`Id`)", sql);
    }
}

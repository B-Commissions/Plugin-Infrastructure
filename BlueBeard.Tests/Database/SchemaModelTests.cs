using System;
using System.Linq;
using BlueBeard.Database;
using BlueBeard.Database.Attributes;
using Xunit;

namespace BlueBeard.Tests.Database;

// ---------------------------------------------------------------------------
// Test entities
// ---------------------------------------------------------------------------

[Table("schema_plain")]
public class PlainEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; }
    public int? Score { get; set; }
    public Guid? Token { get; set; }
    public bool Active { get; set; }
}

[Table("schema_rich")]
public class RichEntity
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Required, MaxLength(64)]
    public string Name { get; set; }

    [Column("score_col", Nullable = true)]
    public int? Score { get; set; }

    [Unique]
    public string SteamId { get; set; }

    [Index]
    public int Level { get; set; }

    [Index(Group = "region_lookup", Order = 1)]
    public int RegionX { get; set; }

    [Index(Group = "region_lookup", Order = 0)]
    public int RegionY { get; set; }

    [DefaultValue(0)]
    public int Balance { get; set; }

    [DefaultValue("guest")]
    public string Rank { get; set; }

    [DefaultValue(true)]
    public bool Enabled { get; set; }

    [DefaultValue(ServerDefault.CurrentTimestamp)]
    public DateTime CreatedAt { get; set; }

    [MaxLength(Text = true)]
    public string Notes { get; set; }
}

[Table("schema_parent")]
public class SchemaParent
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
}

[Table("schema_child")]
public class SchemaChild
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [ForeignKey(typeof(SchemaParent), "Id")] public int ParentId { get; set; }
}

[Table("schema_child_ambiguous")]
public class SchemaChildAmbiguous
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [ForeignKey(typeof(SchemaParent), "Id")] public int ParentA { get; set; }
    [ForeignKey(typeof(SchemaParent), "Id")] public int ParentB { get; set; }
}

// ---------------------------------------------------------------------------
// Migrator normalization + diff logic (phantom-MODIFY regression suite)
// ---------------------------------------------------------------------------

public class MigratorNormalizeTests
{
    [Theory]
    [InlineData("INT NULL", "int")]
    [InlineData("INT NOT NULL", "int")]
    [InlineData("VARCHAR(255) NULL", "varchar(255)")]
    [InlineData("int(11)", "int")]
    [InlineData("bigint(20)", "bigint")]
    [InlineData("smallint(6)", "smallint")]
    [InlineData("tinyint(1)", "tinyint(1)")] // canonical bool storage, must stay distinct
    [InlineData("  INT  ", "int")]
    public void Normalize_Strips_Nullability_And_Display_Widths(string input, string expected)
        => Assert.Equal(expected, Migrator.Normalize(input));

    [Fact]
    public void Nullable_Column_Type_Matches_Information_Schema_Shape()
    {
        // Regression: nullable/converter columns used to render as "INT NULL" while
        // INFORMATION_SCHEMA reports "int", producing a phantom MODIFY on every startup.
        var meta = TableMetadata.For<PlainEntity>();
        var score = meta.GetColumnByPropertyName(nameof(PlainEntity.Score));
        var token = meta.GetColumnByPropertyName(nameof(PlainEntity.Token));

        Assert.True(Migrator.TypesMatch("int", SchemaSync.GetSqlType(score)));
        Assert.True(Migrator.TypesMatch("char(36)", SchemaSync.GetSqlType(token)));
    }

    [Fact]
    public void Unannotated_Nullable_Column_Needs_No_Modify()
    {
        var meta = TableMetadata.For<PlainEntity>();
        var score = meta.GetColumnByPropertyName(nameof(PlainEntity.Score));
        var existing = new Migrator.ExistingColumn { ColumnType = "int", IsNullable = true };

        Assert.False(Migrator.NeedsModify(score, existing, out _));
    }

    [Fact]
    public void Unannotated_Column_Ignores_Nullability_Drift()
    {
        // A legacy DB column that is NOT NULL must not be churned by an entity that
        // never declared nullability.
        var meta = TableMetadata.For<PlainEntity>();
        var name = meta.GetColumnByPropertyName(nameof(PlainEntity.Name));
        var existing = new Migrator.ExistingColumn { ColumnType = "varchar(255)", IsNullable = false };

        Assert.False(Migrator.NeedsModify(name, existing, out _));
    }

    [Fact]
    public void Explicit_Required_Detects_Nullability_Drift()
    {
        var meta = TableMetadata.For<RichEntity>();
        var name = meta.GetColumnByPropertyName(nameof(RichEntity.Name));
        var existing = new Migrator.ExistingColumn { ColumnType = "varchar(64)", IsNullable = true };

        Assert.True(Migrator.NeedsModify(name, existing, out var reason));
        Assert.Contains("nullability", reason);
    }

    [Fact]
    public void Matching_Default_Needs_No_Modify()
    {
        var meta = TableMetadata.For<RichEntity>();
        var balance = meta.GetColumnByPropertyName(nameof(RichEntity.Balance));
        var existing = new Migrator.ExistingColumn { ColumnType = "int", IsNullable = true, ColumnDefault = "0" };

        Assert.False(Migrator.NeedsModify(balance, existing, out _));
    }

    [Fact]
    public void Missing_Default_Detected()
    {
        var meta = TableMetadata.For<RichEntity>();
        var balance = meta.GetColumnByPropertyName(nameof(RichEntity.Balance));
        var existing = new Migrator.ExistingColumn { ColumnType = "int", IsNullable = true, ColumnDefault = null };

        Assert.True(Migrator.NeedsModify(balance, existing, out var reason));
        Assert.Contains("default", reason);
    }

    [Theory]
    [InlineData("CURRENT_TIMESTAMP", "CURRENT_TIMESTAMP")]
    [InlineData("current_timestamp()", "CURRENT_TIMESTAMP")]   // MariaDB spelling
    [InlineData("(CURRENT_TIMESTAMP)", "CURRENT_TIMESTAMP")]   // MariaDB parens
    [InlineData("guest", "'guest'")]                           // schema reports bare, we render quoted
    [InlineData("1", "1")]
    public void DefaultsMatch_Tolerates_Representation_Quirks(string schemaValue, string rendered)
        => Assert.True(Migrator.DefaultsMatch(schemaValue, rendered));

    [Fact]
    public void DefaultsMatch_Detects_Real_Difference()
        => Assert.False(Migrator.DefaultsMatch("0", "1"));
}

// ---------------------------------------------------------------------------
// DDL generation
// ---------------------------------------------------------------------------

public class SchemaDdlTests
{
    [Fact]
    public void MaxLength_Controls_Varchar_Size()
    {
        var meta = TableMetadata.For<RichEntity>();
        Assert.Equal("VARCHAR(64)", SchemaSync.GetSqlType(meta.GetColumnByPropertyName(nameof(RichEntity.Name))));
        Assert.Equal("TEXT", SchemaSync.GetSqlType(meta.GetColumnByPropertyName(nameof(RichEntity.Notes))));
    }

    [Fact]
    public void Unspecified_String_Stays_Varchar255()
    {
        var meta = TableMetadata.For<PlainEntity>();
        Assert.Equal("VARCHAR(255)", SchemaSync.GetSqlType(meta.GetColumnByPropertyName(nameof(PlainEntity.Name))));
    }

    [Fact]
    public void Required_Emits_Not_Null()
    {
        var meta = TableMetadata.For<RichEntity>();
        var def = SchemaSync.GetColumnDefinition(meta.GetColumnByPropertyName(nameof(RichEntity.Name)));
        Assert.Equal("VARCHAR(64) NOT NULL", def);
    }

    [Fact]
    public void Explicit_Nullable_Emits_Null()
    {
        var meta = TableMetadata.For<RichEntity>();
        var def = SchemaSync.GetColumnDefinition(meta.GetColumnByPropertyName(nameof(RichEntity.Score)));
        Assert.Equal("INT NULL", def);
    }

    [Fact]
    public void Unannotated_Column_Has_No_Nullability_Clause()
    {
        var meta = TableMetadata.For<PlainEntity>();
        Assert.Equal("VARCHAR(255)", SchemaSync.GetColumnDefinition(meta.GetColumnByPropertyName(nameof(PlainEntity.Name))));
        Assert.Equal("INT", SchemaSync.GetColumnDefinition(meta.GetColumnByPropertyName(nameof(PlainEntity.Score))));
    }

    [Theory]
    [InlineData(nameof(RichEntity.Balance), "0")]
    [InlineData(nameof(RichEntity.Rank), "'guest'")]
    [InlineData(nameof(RichEntity.Enabled), "1")]
    [InlineData(nameof(RichEntity.CreatedAt), "CURRENT_TIMESTAMP")]
    public void Defaults_Render_As_Sql_Literals(string property, string expected)
    {
        var meta = TableMetadata.For<RichEntity>();
        Assert.Equal(expected, SchemaSync.RenderDefault(meta.GetColumnByPropertyName(property)));
    }

    [Fact]
    public void String_Default_Escapes_Quotes()
    {
        var col = new ColumnInfo
        {
            PropertyName = "X",
            ClrType = typeof(string),
            Default = new DefaultValueAttribute("it's a 'test'")
        };
        Assert.Equal("'it''s a ''test'''", SchemaSync.RenderDefault(col));
    }

    [Fact]
    public void CreateTable_Emits_Indexes_And_Unique_Keys()
    {
        var sql = SchemaSync.GenerateCreateTable(TableMetadata.For<RichEntity>());
        Assert.Contains("UNIQUE KEY `ux_schema_rich_SteamId` (`SteamId`)", sql);
        Assert.Contains("KEY `ix_schema_rich_Level` (`Level`)", sql);
        // Composite respects Order: RegionY (Order 0) before RegionX (Order 1).
        Assert.Contains("KEY `ix_schema_rich_region_lookup` (`RegionY`, `RegionX`)", sql);
    }

    [Fact]
    public void CreateTable_For_Plain_Entity_Is_Schema_Stable()
    {
        var sql = SchemaSync.GenerateCreateTable(TableMetadata.For<PlainEntity>());
        Assert.Equal(
            "CREATE TABLE IF NOT EXISTS `schema_plain` (" +
            "`Id` INT PRIMARY KEY AUTO_INCREMENT, " +
            "`Name` VARCHAR(255), " +
            "`Score` INT, " +
            "`Token` CHAR(36), " +
            "`Active` TINYINT(1));",
            sql);
    }
}

// ---------------------------------------------------------------------------
// Identifier quoting + FK type-token resolution
// ---------------------------------------------------------------------------

public class SqlIdentifierTests
{
    [Fact]
    public void Quote_Doubles_Embedded_Backticks()
        => Assert.Equal("`na``me`", SqlIdentifier.Quote("na`me"));

    [Fact]
    public void Quote_Wraps_Plain_Identifier()
        => Assert.Equal("`players`", SqlIdentifier.Quote("players"));
}

public class ForeignKeyTokenTests
{
    [Fact]
    public void Resolves_Single_Fk_By_Type()
    {
        var meta = TableMetadata.For<SchemaChild>();
        var col = meta.GetForeignKeyColumnTo(typeof(SchemaParent));
        Assert.NotNull(col);
        Assert.Equal(nameof(SchemaChild.ParentId), col.PropertyName);
    }

    [Fact]
    public void Ambiguous_Fk_Returns_Null()
        => Assert.Null(TableMetadata.For<SchemaChildAmbiguous>().GetForeignKeyColumnTo(typeof(SchemaParent)));

    [Fact]
    public void Missing_Fk_Returns_Null()
        => Assert.Null(TableMetadata.For<SchemaParent>().GetForeignKeyColumnTo(typeof(SchemaChild)));
}

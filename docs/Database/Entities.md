# Entities

Entities are plain C# classes that map to MySQL tables. BlueBeard.Database uses attributes to control the mapping. All public properties with both a getter and setter are included as columns, except those marked as navigation properties (`[HasMany]`, `[BelongsTo]`).

---

## Attributes

### [Table("table_name")]

Applied to the class. Sets the MySQL table name.

```csharp
[Table("factions")]
public class Faction { ... }
```

If omitted, the class name is used as the table name.

### [Column("column_name")]

Applied to a property. Sets the MySQL column name.

```csharp
[Column("faction_name")]
public string Name { get; set; }
```

If omitted, the property name is used as the column name.

### [PrimaryKey]

Applied to a property. Marks it as the table's primary key. At most one property should be marked.

```csharp
[PrimaryKey]
public int Id { get; set; }
```

The primary key is required for `UpdateAsync` and `DeleteAsync(entity)`. Both methods throw `InvalidOperationException` if no primary key is defined.

### [AutoIncrement]

Applied to a property (typically alongside `[PrimaryKey]`). Marks the column as `AUTO_INCREMENT` in MySQL.

```csharp
[PrimaryKey]
[AutoIncrement]
public int Id { get; set; }
```

When `InsertAsync` is called, auto-increment columns are excluded from the `INSERT` statement. After the insert, the generated ID is retrieved via `SELECT LAST_INSERT_ID()` and written back to the entity object.

### [ColumnType("SQL_TYPE")]

Applied to a property. Overrides the default SQL type with a custom string. Highest priority — wins over both built-in mappings and converter defaults.

```csharp
[ColumnType("TEXT")]
public string JsonData { get; set; }

[ColumnType("MEDIUMTEXT")]
public string LargePayload { get; set; }
```

This is useful when the default `VARCHAR(255)` is too small for your data, or when you want to force a specific shape on a property whose CLR type has a registered converter (e.g. forcing `BINARY(16)` for a `Guid` column).

### [ColumnConverter(typeof(MyConverter))]

Applied to a property. Forces a specific `IValueConverter` for this column, overriding the registry's default for the property's CLR type.

```csharp
[ColumnConverter(typeof(GuidBinaryConverter))]
public Guid Id { get; set; }   // stored as BINARY(16) instead of CHAR(36)
```

See [Converters](Converters) for the full mechanism.

### [ForeignKey(typeof(Parent), nameof(Parent.Id))]

Applied to a column property. Emits a MySQL `FOREIGN KEY ... REFERENCES` constraint when the table is created.

```csharp
[ForeignKey(typeof(Faction), nameof(Faction.Id), OnDelete = ReferentialAction.Cascade)]
[Column("faction_id")]
public int FactionId { get; set; }
```

Optional `OnDelete` and `OnUpdate` properties accept a `ReferentialAction` (`Restrict`, `Cascade`, `SetNull`, `NoAction`). Both default to `Restrict`.

See [Relationships](Relationships) for caveats including registration order.

### [HasMany("ForeignKeyProperty")]

Marks a `List<T>` (or `IList<T>` / `ICollection<T>` / `IEnumerable<T>`) property as a one-to-many navigation. **Does not map to a column.** Auto-populated when the parent entity is loaded, with a single batched query covering all parents in the result set.

```csharp
[HasMany(nameof(Member.FactionId))]
public List<Member> Members { get; set; }
```

The argument is the property name on the related entity that holds the foreign key. See [Relationships](Relationships).

### [BelongsTo("LocalKeyProperty")]

Marks a property as a many-to-one navigation. **Does not map to a column.** Auto-populated by querying the parent table using the local key value.

```csharp
[BelongsTo(nameof(FactionId))]
public Faction Faction { get; set; }
```

The argument is the property name on this entity that holds the foreign key value.

---

## CLR Type to SQL Type Mappings

The following CLR types map to SQL types automatically. Mapping precedence is `[ColumnType]` > converter default > built-in primitive map > enum fallback.

### Built-in primitives

| CLR Type     | SQL Type           | Notes                                        |
|--------------|--------------------|----------------------------------------------|
| `int`        | `INT`              |                                              |
| `long`       | `BIGINT`           |                                              |
| `ulong`      | `BIGINT UNSIGNED`  |                                              |
| `string`     | `VARCHAR(255)`     | Override with `[ColumnType]` for larger text |
| `bool`       | `TINYINT(1)`       |                                              |
| `float`      | `FLOAT`            |                                              |
| `double`     | `DOUBLE`           |                                              |
| `DateTime`   | `DATETIME`         |                                              |
| `enum`       | `INT`              | Any enum type is stored as its integer value |

### Built-in converter types

These are handled by the converter system rather than the primitive map:

| CLR Type    | Default SQL Type | Converter                              |
|-------------|------------------|----------------------------------------|
| `Guid`      | `CHAR(36)`       | `GuidConverter` (auto-registered)      |
| `Guid`      | `BINARY(16)`     | `GuidBinaryConverter` (opt-in via `[ColumnConverter]`) |
| `byte[]`    | `VARBINARY(255)` | `ByteArrayConverter` (auto-registered) |
| `TimeSpan`  | `BIGINT` (ticks) | `TimeSpanConverter` (auto-registered)  |

You can register your own converters at startup. See [Converters](Converters).

### Nullable Types

`Nullable<T>` (e.g., `int?`, `DateTime?`, `Guid?`) uses the same SQL type as `T` but appends `NULL` to the column definition. Non-nullable columns do not have an explicit `NOT NULL` constraint in the generated DDL -- MySQL defaults apply.

### Unsupported Types

If a property type is not in the primitive map, has no registered converter, is not an enum, and has no `[ColumnType]` override, schema sync throws `NotSupportedException`.

---

## Full Entity Example

```csharp
using System;
using System.Collections.Generic;
using BlueBeard.Database.Attributes;

[Table("factions")]
public class Faction
{
    [PrimaryKey]
    [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("faction_name")]
    public string Name { get; set; }

    [Column("leader_steam_id")]
    public ulong LeaderSteamId { get; set; }

    [Column("public_id")]
    public Guid PublicId { get; set; }   // CHAR(36) via GuidConverter

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("level")]
    public int Level { get; set; }

    [ColumnType("TEXT")]
    [Column("description")]
    public string Description { get; set; }

    [Column("status")]
    public FactionStatus Status { get; set; }

    [Column("disbanded_at")]
    public DateTime? DisbandedAt { get; set; }

    // Navigation: not a column. Auto-populated from `members.faction_id`.
    [HasMany(nameof(Member.FactionId))]
    public List<Member> Members { get; set; }
}

[Table("members")]
public class Member
{
    [PrimaryKey]
    [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("faction_id")]
    [ForeignKey(typeof(Faction), nameof(Faction.Id), OnDelete = ReferentialAction.Cascade)]
    public int FactionId { get; set; }

    [BelongsTo(nameof(FactionId))]
    public Faction Faction { get; set; }   // Navigation back to parent

    [Column("steam_id")]
    public ulong SteamId { get; set; }
}

public enum FactionStatus
{
    Active = 0,
    Inactive = 1,
    Disbanded = 2
}
```

This generates roughly the following DDL:

```sql
CREATE TABLE IF NOT EXISTS `factions` (
    `id` INT PRIMARY KEY AUTO_INCREMENT,
    `faction_name` VARCHAR(255),
    `leader_steam_id` BIGINT UNSIGNED,
    `public_id` CHAR(36),
    `created_at` DATETIME,
    `level` INT,
    `description` TEXT,
    `status` INT,
    `disbanded_at` DATETIME NULL
);

CREATE TABLE IF NOT EXISTS `members` (
    `id` INT PRIMARY KEY AUTO_INCREMENT,
    `faction_id` INT,
    `steam_id` BIGINT UNSIGNED,
    CONSTRAINT `fk_members_faction_id`
        FOREIGN KEY (`faction_id`) REFERENCES `factions`(`id`)
        ON DELETE CASCADE ON UPDATE RESTRICT
);
```

`Members` and `Faction` don't appear in the DDL because they're navigation properties, not columns.

---

## How Metadata Is Built

When a type is first used (via `RegisterEntity<T>`, `Table<T>`, or any `DbSet<T>` operation), `TableMetadata.For(type)` scans the class using reflection:

1. Reads the `[Table]` attribute for the table name (falls back to the class name).
2. Iterates all public instance properties with both getter and setter.
3. For each property:
   - If `[HasMany]` or `[BelongsTo]` is present, captures it as a navigation and skips column mapping.
   - Otherwise reads `[Column]`, `[PrimaryKey]`, `[AutoIncrement]`, `[ColumnType]`, `[ColumnConverter]`, and `[ForeignKey]` attributes.
   - Resolves an `IValueConverter` from `[ColumnConverter]` first, then the type registry as fallback.
4. Builds a `ColumnInfo` list and `NavigationInfo` list, identifies the primary key.

The result is cached in a `ConcurrentDictionary` so reflection only happens once per type.
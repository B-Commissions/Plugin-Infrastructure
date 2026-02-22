# Entities

Entities are plain C# classes that map to MySQL tables. BlueBeard.Database uses attributes to control the mapping. All public properties with both a getter and setter are included as columns.

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

Applied to a property. Overrides the default CLR-to-SQL type mapping with a custom SQL type string.

```csharp
[ColumnType("TEXT")]
public string JsonData { get; set; }

[ColumnType("MEDIUMTEXT")]
public string LargePayload { get; set; }
```

This is useful when the default `VARCHAR(255)` is too small for your data.

---

## CLR Type to SQL Type Mappings

The following mappings are applied automatically when no `[ColumnType]` override is present:

| CLR Type     | SQL Type           | Notes                                        |
|--------------|--------------------|----------------------------------------------|
| `int`        | `INT`              |                                              |
| `long`       | `BIGINT`           |                                              |
| `ulong`      | `BIGINT UNSIGNED`  |                                              |
| `string`     | `VARCHAR(255)`     | Override with `[ColumnType]` for larger text  |
| `bool`       | `TINYINT(1)`       |                                              |
| `float`      | `FLOAT`            |                                              |
| `double`     | `DOUBLE`           |                                              |
| `DateTime`   | `DATETIME`         |                                              |
| `enum`       | `INT`              | Any enum type is stored as its integer value  |

### Nullable Types

`Nullable<T>` (e.g., `int?`, `DateTime?`) uses the same SQL type as `T` but appends `NULL` to the column definition. Non-nullable columns do not have an explicit `NOT NULL` constraint in the generated DDL -- MySQL defaults apply.

### Unsupported Types

If a property type is not in the mapping table, is not an enum, and has no `[ColumnType]` override, schema sync throws `NotSupportedException`.

---

## Full Entity Example

```csharp
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
}

public enum FactionStatus
{
    Active = 0,
    Inactive = 1,
    Disbanded = 2
}
```

This generates the following DDL:

```sql
CREATE TABLE IF NOT EXISTS `factions` (
    `id` INT PRIMARY KEY AUTO_INCREMENT,
    `faction_name` VARCHAR(255),
    `leader_steam_id` BIGINT UNSIGNED,
    `created_at` DATETIME,
    `level` INT,
    `description` TEXT,
    `status` INT,
    `disbanded_at` DATETIME NULL
);
```

---

## How Metadata Is Built

When a type is first used (via `RegisterEntity<T>`, `Table<T>`, or any `DbSet<T>` operation), `TableMetadata.For(type)` scans the class using reflection:

1. Reads the `[Table]` attribute for the table name (falls back to the class name).
2. Iterates all public instance properties with both getter and setter.
3. For each property, reads `[Column]`, `[PrimaryKey]`, `[AutoIncrement]`, and `[ColumnType]` attributes.
4. Builds a `ColumnInfo` list and identifies the primary key.

The result is cached in a `ConcurrentDictionary` so reflection only happens once per type.

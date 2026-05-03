# Migrations

By default, BlueBeard.Database only creates tables that don't yet exist (`CREATE TABLE IF NOT EXISTS`). Once a table exists, schema sync leaves it alone — even if you've added, removed, or retyped properties on the entity since.

`MigrationMode` lets you opt into automatic schema evolution on every `Load()`. The pattern is loosely modelled on Prisma's `db push`: developer-friendly, no migration files, applies on every startup.

---

## The three modes

```csharp
DatabaseManager.RegisterEntity<MyEntity>(MigrationMode.Update);
```

| Mode     | What it does on every `Load`                                                              |
|----------|-------------------------------------------------------------------------------------------|
| `None`   | `CREATE TABLE IF NOT EXISTS` only. Never alters an existing table. **Default.**           |
| `Update` | Creates if missing. On existing tables: ADDs missing columns and MODIFYs type-changed columns. Never drops columns. |
| `Reset`  | `DROP TABLE` then `CREATE`. Wipes all data on every load. **Dev only.**                   |

Each entity carries its own mode. You can mix them — most production tables stay on `None`, while a table you're actively iterating on uses `Update`.

---

## When to use each mode

### `None` (default)

For stable production tables where schema changes go through manual SQL or a real migration tool. No surprises, no risk of accidental ALTERs.

### `Update`

For active development. Add a property to your entity, restart the plugin, the column shows up. Change a property's type, restart, the column gets `MODIFY`'d. Workflow:

```csharp
// Day 1
public class Faction
{
    [PrimaryKey] public int Id { get; set; }
    public string Name { get; set; }
}

// Day 5 — added Level. Restart, ALTER TABLE ADD COLUMN runs automatically.
public class Faction
{
    [PrimaryKey] public int Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
}
```

### `Reset`

For tests or short-lived dev iterations where you don't care about preserving data:

```csharp
DatabaseManager.RegisterEntity<TestData>(MigrationMode.Reset);
```

Every plugin reload gives you a fresh empty table. **Never use this on a production database.**

---

## What `Update` mode does

For each registered entity, the migrator runs `CREATE TABLE IF NOT EXISTS` first (in case the table is brand-new), then queries `INFORMATION_SCHEMA.COLUMNS` to compare existing columns against the entity's metadata:

| Difference                                         | Action                                            |
|----------------------------------------------------|---------------------------------------------------|
| Column in metadata, not in DB                      | `ALTER TABLE ... ADD COLUMN ...`                  |
| Column in both, types match                        | No-op                                             |
| Column in both, types differ                       | `ALTER TABLE ... MODIFY COLUMN ...`               |
| Column in DB, not in metadata                      | **Left alone** (data preservation)                |
| Foreign keys, indexes, constraints on existing tables | **Left alone** (not migrated)                  |

Each ALTER is logged. If a `MODIFY` fails (e.g., MySQL refuses because existing data isn't coercible to the new type), the failure is logged and migration continues with the next column.

### Type comparison

Existing column types come from MySQL's `COLUMN_TYPE`, which is lowercase and may include cosmetic display widths (`bigint(20)`, `int(11)`). The migrator normalizes both sides — lowercases, collapses whitespace, strips display widths from `int`/`bigint`/`smallint`/`mediumint` — so spurious migrations don't fire on every startup. `tinyint(1)` (the canonical bool storage) is left intact because the width is semantically meaningful.

You may still see unnecessary `MODIFY`s in edge cases involving collations, charsets, or other type metadata not in our normalization. Those are non-destructive — MySQL no-ops if the change is truly identical at the storage level.

---

## What `Update` mode does NOT do

- **Drop columns.** A column you've removed from an entity stays in the database. Drop it manually if you want it gone.
- **Drop tables.** A type you've stopped registering doesn't get its table dropped.
- **Add or drop foreign keys** on existing tables. `[ForeignKey]` is only emitted on initial `CREATE`.
- **Add or drop indexes.** No automatic index management at all (yet).
- **Move or rename anything.** Renaming a property creates a new column and leaves the old one behind. Either rename via SQL first, or accept the duplicate.

If any of those matter to you, drop down to manual `ALTER` statements via `DatabaseManager.WithConnectionAsync` or `DbSet<T>.ExecuteSqlAsync`.

---

## Recommended workflow

For a new plugin in active development:

```csharp
DatabaseManager.RegisterEntity<Faction>(MigrationMode.Update);
DatabaseManager.RegisterEntity<Member>(MigrationMode.Update);
```

Iterate freely. Add columns, change types, restart, repeat.

When the schema settles before release, change to `None`:

```csharp
DatabaseManager.RegisterEntity<Faction>();        // MigrationMode.None default
DatabaseManager.RegisterEntity<Member>();
```

Now any further schema changes go through deliberate manual SQL — same model that any production-grade ORM ends up at.

For tests:

```csharp
DatabaseManager.RegisterEntity<TestEntity>(MigrationMode.Reset);
```

Fresh DB on every test run.
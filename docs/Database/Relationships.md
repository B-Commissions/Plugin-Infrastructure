# Relationships

Relationships span two distinct concerns:

- **Foreign keys** are a *database-level* concept — they enforce referential integrity in MySQL via `CONSTRAINT ... FOREIGN KEY ... REFERENCES`.
- **Navigation properties** are a *code-level* concept — they let you load related entities into in-memory object graphs without writing the joins yourself.

You can use either independently, but they're typically used together: declare the FK to keep the database honest, declare the navigation to traverse the relationship in code.

---

## Foreign keys with [ForeignKey]

Apply `[ForeignKey]` to a column property to emit a MySQL constraint when the table is created:

```csharp
[Column("faction_id")]
[ForeignKey(typeof(Faction), nameof(Faction.Id))]
public int FactionId { get; set; }
```

This generates:

```sql
CONSTRAINT `fk_members_faction_id`
    FOREIGN KEY (`faction_id`) REFERENCES `factions`(`id`)
    ON DELETE RESTRICT ON UPDATE RESTRICT
```

### Referential actions

`[ForeignKey]` accepts optional `OnDelete` and `OnUpdate` properties, both of type `ReferentialAction`:

| Action     | SQL          | Behaviour                                                  |
|------------|--------------|------------------------------------------------------------|
| `Restrict` | `RESTRICT`   | Default. Reject the change if dependent rows exist.        |
| `Cascade`  | `CASCADE`    | Apply the change to dependent rows automatically.          |
| `SetNull`  | `SET NULL`   | Null the FK column on dependent rows. Column must allow NULL. |
| `NoAction` | `NO ACTION`  | Same as `Restrict` in InnoDB.                              |

```csharp
[ForeignKey(typeof(Faction), nameof(Faction.Id),
    OnDelete = ReferentialAction.Cascade,
    OnUpdate = ReferentialAction.Restrict)]
public int FactionId { get; set; }
```

### Registration order

Inline `CONSTRAINT ... REFERENCES` requires the parent table to exist at `CREATE TABLE` time. **Register parents before children:**

```csharp
DatabaseManager.RegisterEntity<Faction>();        // parent first
DatabaseManager.RegisterEntity<Member>();         // child second
DatabaseManager.Load();
```

If you get them backwards, you'll see a MySQL error like `Foreign key constraint is incorrectly formed` or `Failed to open the referenced table`.

### Foreign keys and migrations

`[ForeignKey]` is emitted on initial `CREATE TABLE` only. Adding `[ForeignKey]` to an existing table won't generate an `ALTER TABLE ADD CONSTRAINT` — you'd need to run that manually. See [Migrations](Migrations) for what `MigrationMode.Update` does and doesn't cover.

---

## HasMany navigation

`[HasMany]` marks a `List<T>` (or any `IList<T>` / `ICollection<T>` / `IEnumerable<T>`) as a one-to-many navigation. The collection is auto-populated whenever the parent entity is loaded:

```csharp
[Table("factions")]
public class Faction
{
    [PrimaryKey]
    public int Id { get; set; }

    [HasMany(nameof(Member.FactionId))]
    public List<Member> Members { get; set; }
}

[Table("members")]
public class Member
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("faction_id")]
    public int FactionId { get; set; }
}
```

Loading a `Faction` automatically populates `Members` with all matching rows from `members`:

```csharp
var faction = await db.Table<Faction>().FirstOrDefaultAsync(f => f.Id == 1);
foreach (var m in faction.Members)
{
    // ...
}
```

The argument to `[HasMany]` is the property name on the *related* entity that holds the foreign key — i.e., `Member.FactionId`, the property that points back at `Faction.Id`.

### Batched loading (no N+1)

When a query returns multiple parents, navigation children are fetched with a single batched query:

```sql
SELECT * FROM `members` WHERE `faction_id` IN (@k0, @k1, @k2, ...);
```

So a query that returns 100 factions does one extra round-trip to load all members across all factions, not 100. Children are then grouped by FK and assigned to the right parent.

### Empty collections

If a parent has no matching children, the navigation property is set to an empty `List<T>` — never `null`. You can iterate without nullchecks.

---

## BelongsTo navigation

`[BelongsTo]` is the inverse: a single-valued reference from child to parent.

```csharp
[Table("members")]
public class Member
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("faction_id")]
    public int FactionId { get; set; }

    [BelongsTo(nameof(FactionId))]
    public Faction Faction { get; set; }
}
```

The argument to `[BelongsTo]` is the property name on *this* entity that holds the foreign key. Loading members auto-populates `Faction`:

```csharp
var members = await db.Table<Member>().Where(m => m.SteamId == steamId);
foreach (var m in members)
{
    Console.WriteLine($"{m.SteamId} is in {m.Faction.Name}");
}
```

Like `HasMany`, this uses a single batched `WHERE pk IN (...)` query covering all distinct FK values in the result set.

---

## Combining HasMany and BelongsTo

You can declare both directions on a relationship. They don't interfere — each navigation is loaded with its own query, in the direction it's declared:

```csharp
[Table("factions")]
public class Faction
{
    [PrimaryKey]
    public int Id { get; set; }

    [HasMany(nameof(Member.FactionId))]
    public List<Member> Members { get; set; }
}

[Table("members")]
public class Member
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("faction_id")]
    [ForeignKey(typeof(Faction), nameof(Faction.Id), OnDelete = ReferentialAction.Cascade)]
    public int FactionId { get; set; }

    [BelongsTo(nameof(FactionId))]
    public Faction Faction { get; set; }
}
```

Loading a `Faction` populates `Members`, but those `Member` instances will *not* recursively re-populate their `Faction` back-reference — navigation queries don't recurse. You'd need a third query for that, which the library doesn't issue automatically (and shouldn't, to avoid runaway loading on cyclic models).

If you need the child entity to know about its parent in this scenario, set the back-reference yourself, or query the children separately.

---

## When navigation runs

Navigations are populated by every method on `DbSet<T>` that returns hydrated entities:

- `QueryAsync()`
- `Where(predicate)`
- `FirstOrDefaultAsync(predicate)`
- `QuerySqlAsync(sql, ...)`

It runs after the main result set is read and the reader is closed, on the same connection. There's currently no way to skip navigation loading per-call — if you've annotated the property, it loads. If you don't want it loaded, don't annotate it (or comment the attribute out temporarily).

---

## Caveats

- **No FK migration.** `MigrationMode.Update` doesn't add or drop foreign key constraints on existing tables. FKs are only emitted on initial table creation.
- **Single-column keys only.** Composite primary keys aren't supported, so foreign keys to composite parents aren't either.
- **No cycle detection on save.** Inserting a parent with `Members` already populated does *not* cascade-insert the children. You insert each entity explicitly.
- **No lazy loading.** Navigation properties are loaded eagerly when entities are read. If a parent has 50,000 children, all 50,000 are loaded. Use `QuerySqlAsync` with explicit joins for cases where you need finer control.
- **Writes don't traverse navigations.** `UpdateAsync(faction)` does not update `faction.Members`. Update each entity in its own `DbSet<T>` call.
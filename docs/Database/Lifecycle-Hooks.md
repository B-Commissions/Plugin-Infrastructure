# Lifecycle Hooks

Entity methods tagged with hook attributes run automatically around DbSet operations —
timestamps, clamping, cache invalidation, audit trails — without wrapping every call site.

## The six stages

`[BeforeInsert]`, `[AfterInsert]`, `[BeforeUpdate]`, `[AfterUpdate]`, `[BeforeDelete]`,
`[AfterDelete]` — named after the `DbSet` verbs.

- **Before\*** runs before the SQL is sent. Mutations the hook makes to the entity ARE
  written.
- **After\*** runs only after the operation succeeds (for inserts: after the
  auto-increment ID is assigned back).
- Multiple hooks per entity are allowed and run in declaration order.
- Methods may return `void` or `Task` (awaited).

## Entity-level hooks

Parameterless instance methods (private is fine):

```csharp
[Table("players")]
public class PlayerData
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }

    [BeforeInsert]
    [BeforeUpdate]
    private void Touch() => UpdatedAt = DateTime.UtcNow;

    [AfterInsert]
    private Task Announce() => Broadcast.NewPlayerAsync(this);
}
```

## Column-targeted hooks (typed values)

The attribute names a column and the method takes exactly one parameter of the column's
real CLR type — `string value`, `int value`, etc., never `object`:

```csharp
[Column("balance")] public int Balance { get; set; }
[Column("player_name")] public string PlayerName { get; set; }

[BeforeUpdate("balance")]
private void ClampBalance(int value) { if (value < 0) Balance = 0; }

[AfterInsert("player_name")]
private void OnNamed(string value) => NameIndex.Add(value, Id);
```

Rules:

- The target matches the **column name** (your `[Column("...")]` literal), with the
  property name as a fallback. Use the column literal — property-name strings do not
  survive obfuscation; column-name literals do.
- The parameter type is validated when the entity's metadata is built. A mismatch throws
  at `RegisterEntity` with a precise message — not at query time.
- One method may carry several hook attributes (different stages and/or columns), as long
  as its single parameter type matches every targeted column.
- A `null` column value is skipped for hooks whose parameter is a non-nullable value type
  (an `int value` hook can't receive a null `int?` column).

## Semantics and limits

- Hooks fire on instance operations: `InsertAsync`, `UpdateAsync`, `DeleteAsync(entity)`,
  and the range variants (per entity).
- **Expression deletes bypass hooks** — `DeleteAsync(x => ...)` has no entity instances to
  call them on (same semantics as EF bulk operations). Use the entity overload when hooks
  matter.
- The ORM has no change tracking: column-targeted Update hooks fire on every update with
  the value being written, not only when it changed.
- Hook dispatch uses invokers compiled once at metadata build — no per-call reflection.

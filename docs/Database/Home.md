# BlueBeard.Database Wiki

BlueBeard.Database is a lightweight MySQL ORM for Unturned plugins running RocketMod. It maps C# classes to MySQL tables using attributes and provides async CRUD operations with LINQ-expression-to-SQL translation.

---

## Pages

- [Getting Started](Getting-Started) -- setup, initialization, and first query
- [Entities](Entities) -- defining entity classes with attributes
- [Queries](Queries) -- CRUD operations reference, including the raw-SQL escape hatches
- [Converters](Converters) -- mapping non-primitive CLR types (Guid, byte[], TimeSpan, custom)
- [Relationships](Relationships) -- foreign keys, `HasMany`, `BelongsTo`
- [Migrations](Migrations) -- schema evolution via `MigrationMode` and versioned `IMigration` steps
- [Lifecycle Hooks](Lifecycle-Hooks) -- Before/After Insert/Update/Delete entity callbacks
- [Examples](Examples) -- full plugin implementation examples

---

## Features

- **Attribute-based entity mapping** using `[Table]`, `[Column]`, `[PrimaryKey]`, `[AutoIncrement]`, `[ColumnType]`, `[ColumnConverter]`, and `[ForeignKey]`
- **Full column model** — `[Required]`/`[Column(Nullable = ...)]`, `[DefaultValue]`, `[Unique]`, `[Index]` (incl. composites), `[MaxLength]`
- **Async CRUD** via `DbSet<T>`: `QueryAsync`, `Where`, `FirstOrDefaultAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `CountAsync`, `AnyAsync`
- **Composable queries** via `Query()` — chained `Where`, `OrderBy`/`ThenBy`, `Take`/`Skip`
- **Transactions** via `BeginTransactionAsync` with overloads on every read/write; **batch ops** via `InsertRangeAsync`/`UpdateRangeAsync`
- **Lifecycle hooks** — `[BeforeInsert]`/`[AfterUpdate]`/etc. entity methods, including column-targeted hooks with typed parameters
- **LINQ expression to SQL** translation for `WHERE` clauses — comparisons, `Contains`/`StartsWith`/`EndsWith` (`LIKE`), `string.IsNullOrEmpty`, collection `Contains` (`IN`), parameterized and converter-aware
- **Type converters** for non-primitive types — built-ins for `Guid`, `byte[]`, `TimeSpan`; custom converters via `IValueConverter`
- **Schema migrations** via `MigrationMode.Update` (independent type/nullability/default diffing, index creation) plus versioned run-once `IMigration` steps
- **Foreign keys** declared via `[ForeignKey]`, emitted as MySQL `CONSTRAINT` clauses; type-token forms survive obfuscation
- **Navigation properties** (`[HasMany]`, `[BelongsTo]`) auto-populated with batched `WHERE pk IN (...)` queries — no N+1
- **Transient-fault retry** — deadlocks and lock-wait timeouts retry automatically outside caller-owned transactions
- **Raw SQL escape hatches** -- `QuerySqlAsync`, `ExecuteSqlAsync`, `WithConnectionAsync`, `CreateConnection` for whatever the expression visitor can't translate
- **Built on MySqlConnector** for reliable async MySQL access
- **Background-thread safe** -- pairs with `ThreadHelper` for non-blocking database calls; `LoadAsync` for fully async startup
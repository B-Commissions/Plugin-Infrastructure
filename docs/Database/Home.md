# BlueBeard.Database Wiki

BlueBeard.Database is a lightweight MySQL ORM for Unturned plugins running RocketMod. It maps C# classes to MySQL tables using attributes and provides async CRUD operations with LINQ-expression-to-SQL translation.

---

## Pages

- [Getting Started](Getting-Started) -- setup, initialization, and first query
- [Entities](Entities) -- defining entity classes with attributes
- [Queries](Queries) -- CRUD operations reference, including the raw-SQL escape hatches
- [Converters](Converters) -- mapping non-primitive CLR types (Guid, byte[], TimeSpan, custom)
- [Relationships](Relationships) -- foreign keys, `HasMany`, `BelongsTo`
- [Migrations](Migrations) -- schema evolution via `MigrationMode`
- [Examples](Examples) -- full plugin implementation examples

---

## Features

- **Attribute-based entity mapping** using `[Table]`, `[Column]`, `[PrimaryKey]`, `[AutoIncrement]`, `[ColumnType]`, `[ColumnConverter]`, and `[ForeignKey]`
- **Async CRUD** via `DbSet<T>`: `QueryAsync`, `Where`, `FirstOrDefaultAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`
- **LINQ expression to SQL** translation for `WHERE` clauses with parameterized queries and converter-aware parameter binding
- **Type converters** for non-primitive types — built-ins for `Guid`, `byte[]`, `TimeSpan`; custom converters via `IValueConverter`
- **Schema migrations** via `MigrationMode.Update` — additively adds and modifies columns on every load (Prisma `db push` style)
- **Foreign keys** declared via `[ForeignKey]`, emitted as MySQL `CONSTRAINT` clauses
- **Navigation properties** (`[HasMany]`, `[BelongsTo]`) auto-populated with batched `WHERE pk IN (...)` queries — no N+1
- **Raw SQL escape hatches** -- `QuerySqlAsync`, `ExecuteSqlAsync`, `WithConnectionAsync`, `CreateConnection` for whatever the expression visitor can't translate
- **Built on MySqlConnector** for reliable async MySQL access
- **Background-thread safe** -- pairs with `ThreadHelper` for non-blocking database calls
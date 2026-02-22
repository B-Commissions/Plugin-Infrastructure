# BlueBeard.Database Wiki

BlueBeard.Database is a lightweight MySQL ORM for Unturned plugins running RocketMod. It maps C# classes to MySQL tables using attributes and provides async CRUD operations with LINQ-expression-to-SQL translation.

---

## Pages

- [Getting Started](Getting-Started) -- setup, initialization, and first query
- [Entities](Entities) -- defining entity classes with attributes
- [Queries](Queries) -- CRUD operations reference
- [Examples](Examples) -- full plugin implementation examples

---

## Features

- **Attribute-based entity mapping** using `[Table]`, `[Column]`, `[PrimaryKey]`, `[AutoIncrement]`, and `[ColumnType]`
- **Async CRUD** via `DbSet<T>`: `QueryAsync`, `Where`, `FirstOrDefaultAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`
- **LINQ expression to SQL** translation for `WHERE` clauses with parameterized queries
- **Automatic schema creation** -- `CREATE TABLE IF NOT EXISTS` on startup
- **Supported CLR types**: `int`, `long`, `ulong`, `string` (VARCHAR 255), `bool`, `float`, `double`, `DateTime`, enums (stored as INT), and their `Nullable<T>` variants
- **Built on MySqlConnector** for reliable async MySQL access
- **Background-thread safe** -- pairs with `ThreadHelper` for non-blocking database calls

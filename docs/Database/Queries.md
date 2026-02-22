# Queries

`DbSet<T>` is the main API for all CRUD operations. Obtain one via `DatabaseManager.Table<T>()`. Every method is async, opens its own connection, and disposes it when done.

**All operations must be called from a background thread.** Use `ThreadHelper.RunAsynchronously` to run database code off the main thread, and `ThreadHelper.RunSynchronously` to dispatch results back to the main thread.

---

## QueryAsync

Retrieves all rows from the table.

**Signature:**
```csharp
Task<List<T>> QueryAsync()
```

**Generated SQL:**
```sql
SELECT * FROM `table_name`;
```

**Example:**
```csharp
var factions = _databaseManager.Table<Faction>();
List<Faction> all = await factions.QueryAsync();
```

---

## Where

Retrieves rows matching a LINQ expression predicate.

**Signature:**
```csharp
Task<List<T>> Where(Expression<Func<T, bool>> predicate)
```

**Generated SQL:**
```sql
SELECT * FROM `table_name` WHERE <translated_expression>;
```

**Examples:**
```csharp
var factions = _databaseManager.Table<Faction>();

// Simple equality
var active = await factions.Where(f => f.Status == FactionStatus.Active);

// Comparison operators
var highLevel = await factions.Where(f => f.Level >= 5);

// Compound conditions with AND
var result = await factions.Where(f => f.Level >= 5 && f.Status == FactionStatus.Active);

// OR conditions
var result = await factions.Where(f => f.Level >= 10 || f.Status == FactionStatus.Inactive);

// Variable capture (parameterized)
ulong steamId = 76561198012345678;
var mine = await factions.Where(f => f.LeaderSteamId == steamId);

// Null checks
var disbanded = await factions.Where(f => f.DisbandedAt != null);
var notDisbanded = await factions.Where(f => f.DisbandedAt == null);
```

---

## FirstOrDefaultAsync

Retrieves the first row matching a predicate, or `default(T)` if no match is found.

**Signature:**
```csharp
Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
```

**Generated SQL:**
```sql
SELECT * FROM `table_name` WHERE <translated_expression> LIMIT 1;
```

**Example:**
```csharp
var factions = _databaseManager.Table<Faction>();
Faction faction = await factions.FirstOrDefaultAsync(f => f.Name == "Raiders");

if (faction == null)
{
    // Not found
}
```

---

## InsertAsync

Inserts a new row. Auto-increment columns are excluded from the INSERT and their generated value is written back to the entity after insertion.

**Signature:**
```csharp
Task InsertAsync(T entity)
```

**Generated SQL:**
```sql
INSERT INTO `table_name` (col1, col2, ...) VALUES (@p0, @p1, ...); SELECT LAST_INSERT_ID();
```

**Example:**
```csharp
var factions = _databaseManager.Table<Faction>();
var faction = new Faction
{
    Name = "Raiders",
    LeaderSteamId = 76561198012345678,
    CreatedAt = DateTime.UtcNow,
    Level = 1,
    Status = FactionStatus.Active
};

await factions.InsertAsync(faction);

// faction.Id is now populated with the auto-generated value
Console.WriteLine($"Created faction with ID: {faction.Id}");
```

---

## UpdateAsync

Updates an existing row identified by its primary key. All non-primary-key columns are included in the SET clause.

**Signature:**
```csharp
Task UpdateAsync(T entity)
```

**Generated SQL:**
```sql
UPDATE `table_name` SET col1 = @p0, col2 = @p1, ... WHERE `pk_column` = @pN;
```

**Throws:** `InvalidOperationException` if the entity type has no `[PrimaryKey]` defined.

**Example:**
```csharp
var factions = _databaseManager.Table<Faction>();
var faction = await factions.FirstOrDefaultAsync(f => f.Name == "Raiders");

faction.Level = 5;
faction.Description = "A fearsome group of raiders.";

await factions.UpdateAsync(faction);
```

---

## DeleteAsync (by entity)

Deletes the row matching the entity's primary key value.

**Signature:**
```csharp
Task DeleteAsync(T entity)
```

**Generated SQL:**
```sql
DELETE FROM `table_name` WHERE `pk_column` = @p0;
```

**Throws:** `InvalidOperationException` if the entity type has no `[PrimaryKey]` defined.

**Example:**
```csharp
var factions = _databaseManager.Table<Faction>();
var faction = await factions.FirstOrDefaultAsync(f => f.Name == "Raiders");

await factions.DeleteAsync(faction);
```

---

## DeleteAsync (by expression)

Deletes all rows matching a predicate expression. Does not require a primary key.

**Signature:**
```csharp
Task DeleteAsync(Expression<Func<T, bool>> predicate)
```

**Generated SQL:**
```sql
DELETE FROM `table_name` WHERE <translated_expression>;
```

**Example:**
```csharp
var factions = _databaseManager.Table<Faction>();

// Delete all disbanded factions
await factions.DeleteAsync(f => f.Status == FactionStatus.Disbanded);

// Delete by a captured variable
ulong steamId = 76561198012345678;
await factions.DeleteAsync(f => f.LeaderSteamId == steamId);
```

---

## Expression Support

The `SqlWhereVisitor` translates LINQ expressions into parameterized SQL WHERE clauses. The following operators and patterns are supported:

| C# Expression       | SQL Output          |
|----------------------|---------------------|
| `==`                 | `=`                 |
| `!=`                 | `!=`                |
| `>`                  | `>`                 |
| `<`                  | `<`                 |
| `>=`                 | `>=`                |
| `<=`                 | `<=`                |
| `&&`                 | `AND`               |
| `\|\|`              | `OR`                |
| `== null`            | `IS NULL`           |
| `!= null`            | `IS NOT NULL`       |
| `!expression`        | `NOT expression`    |

**Variable capture:** When you reference a local variable or field in the expression (e.g., `f => f.SteamId == steamId`), the visitor evaluates the variable at execution time and adds it as a parameter (`@p0`, `@p1`, etc.). This prevents SQL injection.

**Type conversions:** Implicit casts (e.g., enum to int) are handled transparently via the `Convert` unary expression visitor.

---

## Threading

All `DbSet<T>` methods are `async Task` and must be awaited on a background thread. Never call them from the main Unturned thread.

### Pattern: Background work with main-thread callback

```csharp
ThreadHelper.RunAsynchronously(async () =>
{
    var factions = _databaseManager.Table<Faction>();
    var list = await factions.QueryAsync();

    ThreadHelper.RunSynchronously(() =>
    {
        // Back on the main thread -- safe to use Unturned APIs
        foreach (var faction in list)
        {
            Rocket.Core.Logging.Logger.Log($"Faction: {faction.Name} (Level {faction.Level})");
        }
    });
});
```

### Pattern: Fire-and-forget write

```csharp
ThreadHelper.RunAsynchronously(async () =>
{
    var factions = _databaseManager.Table<Faction>();
    await factions.InsertAsync(new Faction
    {
        Name = "New Faction",
        LeaderSteamId = player.CSteamID.m_SteamID,
        CreatedAt = DateTime.UtcNow,
        Level = 1,
        Status = FactionStatus.Active
    });
}, "Failed to insert faction");
```

The second parameter to `RunAsynchronously` is an optional exception message that is logged if the operation fails.

# BlueBeard.Database

A lightweight MySQL ORM for Unturned plugins. Define entity classes with attributes, and use LINQ-style expressions for queries -- the ORM translates them to SQL automatically.

## Installation

Add a project reference:

```xml
<ProjectReference Include="..\BlueBeard.Database\BlueBeard.Database.csproj" />
```

Requires [MySqlConnector](https://mysqlconnector.net/) (included in the packages folder).

## Setup

```csharp
using BlueBeard.Core.Configs;
using BlueBeard.Database;

// In your plugin's Load():
var configManager = new ConfigManager();
configManager.Initialize(Directory);
configManager.LoadConfig<DatabaseConfig>();

var db = new DatabaseManager();
db.Initialize(configManager);
db.RegisterEntity<Player>();
db.RegisterEntity<Faction>();
db.Load(); // connects and syncs schema
```

`DatabaseConfig` stores connection details (host, port, database, username, password) and is created automatically with defaults on first run.

## Defining Entities

```csharp
using BlueBeard.Database.Attributes;

[Table("factions")]
public class Faction
{
    [PrimaryKey] [AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("leader_steam_id")]
    public ulong LeaderSteamId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
```

### Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[Table("name")]` | Class | Maps the class to a MySQL table. Defaults to the class name if omitted. |
| `[Column("name")]` | Property | Maps the property to a column. Defaults to the property name if omitted. |
| `[PrimaryKey]` | Property | Marks the primary key column. |
| `[AutoIncrement]` | Property | Marks the column as AUTO_INCREMENT. Value is set after insert. |

### Supported Types

`int`, `long`, `ulong`, `string` (VARCHAR 255), `bool`, `float`, `double`, `DateTime`, and any `enum` (stored as INT).

## Schema Sync

Tables are created automatically via `CREATE TABLE IF NOT EXISTS` when `Load()` is called. No migrations needed for new tables.

## CRUD Operations

All operations are async and should be called from a background thread (use `ThreadHelper.RunAsynchronously`):

### Query All

```csharp
var factions = await db.Table<Faction>().QueryAsync();
```

### Query with Filter

```csharp
// LINQ expression is translated to SQL WHERE clause:
var results = await db.Table<Faction>().Where(f => f.LeaderSteamId == steamId);
```

### First or Default

```csharp
var faction = await db.Table<Faction>().FirstOrDefaultAsync(f => f.Name == "Wolves");
```

### Insert

```csharp
var faction = new Faction { Name = "Wolves", LeaderSteamId = 76561198012345678 };
await db.Table<Faction>().InsertAsync(faction);
// faction.Id is now set (auto-increment)
```

### Update

```csharp
faction.Name = "Alpha Wolves";
await db.Table<Faction>().UpdateAsync(faction);
```

### Delete

```csharp
// By entity:
await db.Table<Faction>().DeleteAsync(faction);

// By predicate:
await db.Table<Faction>().DeleteAsync(f => f.Id == 5);
```

## Expression Support

The `Where` and `FirstOrDefaultAsync` methods accept C# lambda expressions that are translated to SQL:

```csharp
// Equality
f => f.Name == "Wolves"           // WHERE `name` = @p0

// Comparison
f => f.Id > 10                    // WHERE `id` > @p0

// Compound
f => f.Id > 5 && f.Name != null   // WHERE (`id` > @p0 AND `name` IS NOT NULL)

// Or
f => f.Id == 1 || f.Id == 2       // WHERE (`id` = @p0 OR `id` = @p1)

// Variable capture
var name = "Wolves";
f => f.Name == name                // WHERE `name` = @p0 (parameterized)
```

## Full Example

```csharp
ThreadHelper.RunAsynchronously(async () =>
{
    var faction = await db.Table<Faction>()
        .FirstOrDefaultAsync(f => f.LeaderSteamId == player.CSteamID.m_SteamID);

    ThreadHelper.RunSynchronously(() =>
    {
        if (faction != null)
            UnturnedChat.Say(player, $"Your faction: {faction.Name}");
        else
            UnturnedChat.Say(player, "You don't have a faction.");
    });
});
```

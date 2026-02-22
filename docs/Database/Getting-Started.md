# Getting Started

This page covers adding BlueBeard.Database to your plugin, configuring a MySQL connection, and running your first query.

---

## 1. Add Project References

Your plugin project needs references to both **BlueBeard.Core** and **BlueBeard.Database**:

```xml
<ItemGroup>
  <ProjectReference Include="..\BlueBeard.Core\BlueBeard.Core.csproj" />
  <ProjectReference Include="..\BlueBeard.Database\BlueBeard.Database.csproj" />
</ItemGroup>
```

BlueBeard.Database also depends on the `MySqlConnector` NuGet package (version 2.5.0), which is included transitively.

---

## 2. DatabaseConfig

`DatabaseConfig` implements `IConfig` and holds the MySQL connection parameters:

| Property   | Type     | Default     |
|------------|----------|-------------|
| `Host`     | `string` | `localhost` |
| `Port`     | `ushort` | `3306`      |
| `Database` | `string` | `unturned`  |
| `Username` | `string` | `root`      |
| `Password` | `string` | *(empty)*   |

When `LoadDefaults()` is called (e.g., when no config file exists yet), these defaults are applied. The config is stored as an XML file at `<PluginDirectory>/Configs/DatabaseConfig.configuration.xml` by `ConfigManager`.

---

## 3. Initialization

Set up the database system in your plugin's `Load` method:

```csharp
using BlueBeard.Core.Configs;
using BlueBeard.Database;

public class MyPlugin : RocketPlugin<MyPluginConfig>
{
    private ConfigManager _configManager;
    private DatabaseManager _databaseManager;

    protected override void Load()
    {
        // 1. Initialize ConfigManager with the plugin directory
        _configManager = new ConfigManager();
        _configManager.Initialize(Directory);

        // 2. Load the database config from disk (or create defaults)
        _configManager.LoadConfig<DatabaseConfig>();

        // 3. Create and initialize DatabaseManager
        _databaseManager = new DatabaseManager();
        _databaseManager.Initialize(_configManager);

        // 4. Register all entity types BEFORE calling Load
        _databaseManager.RegisterEntity<PlayerData>();
        _databaseManager.RegisterEntity<Faction>();

        // 5. Load -- builds the connection string and syncs schema
        _databaseManager.Load();
    }

    protected override void Unload()
    {
        _databaseManager.Unload();
    }
}
```

### What happens during Load()

1. `GetConfig<DatabaseConfig>()` retrieves the loaded config.
2. A `MySqlConnectionStringBuilder` builds the connection string from the config values.
3. `SyncSchema()` runs on a **background thread** via `ThreadHelper.RunAsynchronously`. For every registered entity type, it generates a `CREATE TABLE IF NOT EXISTS` statement and executes it. Each successful table creation is logged as `[Database] Ensured table: <table_name>`.

### What happens during Unload()

The internal `DbSet<T>` cache is cleared. No connections are held open between operations (each method opens and disposes its own connection).

---

## 4. Getting a DbSet

After `Load()`, retrieve a `DbSet<T>` for any registered entity:

```csharp
DbSet<PlayerData> players = _databaseManager.Table<PlayerData>();
```

The `DbSet<T>` is cached per type -- calling `Table<T>()` multiple times returns the same instance.

---

## 5. Quick Example

```csharp
ThreadHelper.RunAsynchronously(async () =>
{
    var players = _databaseManager.Table<PlayerData>();

    // Insert
    var data = new PlayerData { SteamId = 76561198012345678, Name = "Alice" };
    await players.InsertAsync(data);

    // Query
    var all = await players.QueryAsync();

    // Find one
    var alice = await players.FirstOrDefaultAsync(p => p.SteamId == 76561198012345678);

    // Update
    alice.Name = "Alice (updated)";
    await players.UpdateAsync(alice);

    // Delete
    await players.DeleteAsync(alice);
});
```

All database operations are async and must run off the main thread. See [Queries](Queries) for the full API reference.

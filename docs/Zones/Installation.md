# Installation

## As a Standalone Plugin

1. Download the latest release.
2. Place the following DLLs into your server's `Rocket/Plugins/` folder:
   - `BlueBeard.Zones.dll`
   - `BlueBeard.Core.dll`
   - `BlueBeard.Commands.dll`
   - `BlueBeard.Database.dll` (only needed if using MySQL storage)
   - `Newtonsoft.Json.dll`
3. Start the server. The plugin will generate its default configuration files on first load.

## As a Library for Other Plugins

Add a project reference in your `.csproj`:

```xml
<ProjectReference Include="..\BlueBeard.Zones\BlueBeard.Zones.csproj" />
```

Then access the managers from your code:

```csharp
var zoneManager = ZonesPlugin.Instance.ZoneManager;
var tracker = ZonesPlugin.Instance.PlayerTracker;
var blockLists = ZonesPlugin.Instance.BlockListManager;
```

## Dependencies

| Dependency | Required | Purpose |
|---|---|---|
| BlueBeard.Core | Always | Configuration, helpers, IManager lifecycle |
| BlueBeard.Commands | Always | Command framework |
| BlueBeard.Database | MySQL only | MySQL ORM for persistent storage |
| Newtonsoft.Json | Always | JSON serialization for zone data |
| RocketMod | Always | Plugin framework |

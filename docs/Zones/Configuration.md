# Configuration

On first load, the plugin creates a configuration file at:
```
Plugins/BlueBeard.Zones/Configs/ZonesConfig.configuration.xml
```

## Settings

```xml
<?xml version="1.0" encoding="utf-8"?>
<ZonesConfig>
  <StorageType>json</StorageType>
  <EnableFlagEnforcement>true</EnableFlagEnforcement>
</ZonesConfig>
```

### StorageType

Controls where zones and block lists are persisted.

| Value | Description |
|---|---|
| `json` (default) | Stores everything in a single `zones.json` file in the plugin directory. |
| `mysql` | Stores data in MySQL tables `bb_zones` and `bb_zone_blocklists`. |

### EnableFlagEnforcement

| Value | Description |
|---|---|
| `true` (default) | The plugin subscribes to Unturned events and enforces all flags (no-damage, no-build, etc.). |
| `false` | Flag enforcement is disabled. Zones still work for enter/exit detection, but flags have no effect. Useful in library-only mode where another plugin handles its own logic. |

## MySQL Configuration

When `StorageType` is set to `mysql`, the plugin will also load a database configuration file at:
```
Plugins/BlueBeard.Zones/Configs/DatabaseConfig.configuration.xml
```

```xml
<?xml version="1.0" encoding="utf-8"?>
<DatabaseConfig>
  <Host>localhost</Host>
  <Port>3306</Port>
  <Database>unturned</Database>
  <Username>root</Username>
  <Password></Password>
</DatabaseConfig>
```

The plugin will automatically create the required tables on startup:
- `bb_zones` -- stores zone definitions with JSON columns for shape data, flags, and metadata
- `bb_zone_blocklists` -- stores block list names and their item lists as JSON

## JSON Storage

When using JSON storage (the default), all data is stored in:
```
Plugins/BlueBeard.Zones/zones.json
```

Example file:
```json
{
  "zones": [
    {
      "Id": "safezone",
      "CenterX": 100.0,
      "CenterY": 50.0,
      "CenterZ": 200.0,
      "ShapeType": "radius",
      "ShapeData": "{\"radius\":50.0,\"height\":30.0}",
      "FlagsJson": "{\"noDamage\":\"\",\"noBuild\":\"\"}",
      "MetadataJson": null,
      "LowerHeight": null,
      "UpperHeight": null,
      "Priority": 0
    }
  ],
  "blockLists": [
    {
      "Name": "weapons",
      "Items": [363, 519]
    }
  ]
}
```

You can manually edit this file while the server is stopped, but avoid editing it while the server is running.

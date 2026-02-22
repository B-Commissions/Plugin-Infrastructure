# Storage

BlueBeard.Zones supports two storage backends for persisting zones and block lists: JSON files and MySQL. The storage type is configured in `ZonesConfig.configuration.xml`.

## IZoneRepository Interface

All storage backends implement the `IZoneRepository` interface:

```csharp
public interface IZoneRepository
{
    Task<List<ZoneDefinition>> LoadAllAsync();
    Task SaveAsync(ZoneDefinition definition);
    Task DeleteAsync(string id);
    Task<List<BlockList>> LoadAllBlockListsAsync();
    Task SaveBlockListAsync(BlockList blockList);
    Task DeleteBlockListAsync(string name);
}
```

## JSON Storage (Default)

File: `Plugins/BlueBeard.Zones/zones.json`

- Single file containing all zones and block lists
- Thread-safe with a semaphore for concurrent access
- Human-readable and manually editable (when the server is stopped)
- No external dependencies

### File Structure

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
      "FlagsJson": "{\"noDamage\":\"\",\"enterMessage\":\"Welcome!\"}",
      "MetadataJson": null,
      "LowerHeight": null,
      "UpperHeight": null,
      "Priority": 0
    }
  ],
  "blockLists": [
    {
      "Name": "weapons",
      "Items": [363, 519, 1244]
    }
  ]
}
```

## MySQL Storage

Tables: `bb_zones` and `bb_zone_blocklists`

- Shared database for multi-server setups
- Automatic schema creation on startup
- Uses `TEXT` columns for JSON payloads (shape data, flags, metadata)
- Requires the `BlueBeard.Database` project

### Table Schemas

**bb_zones**

| Column | Type | Description |
|---|---|---|
| `id` | `VARCHAR(255)` PK | Zone ID |
| `center_x` | `FLOAT` | X coordinate |
| `center_y` | `FLOAT` | Y coordinate |
| `center_z` | `FLOAT` | Z coordinate |
| `shape_type` | `VARCHAR(255)` | `"radius"` or `"polygon"` |
| `shape_data` | `TEXT` | JSON shape parameters |
| `flags_json` | `TEXT` | JSON flag dictionary |
| `metadata_json` | `TEXT` | JSON metadata dictionary |
| `lower_height` | `FLOAT NULL` | Lower height bound |
| `upper_height` | `FLOAT NULL` | Upper height bound |
| `priority` | `INT` | Zone priority |

**bb_zone_blocklists**

| Column | Type | Description |
|---|---|---|
| `name` | `VARCHAR(255)` PK | Block list name |
| `items_json` | `TEXT` | JSON array of item IDs |

## Serialization

The `ZoneStorageMapper` handles conversion between `ZoneDefinition` and the flat `ZoneStorageData` DTO:

- Shapes are serialized by their `ShapeType` discriminator
- Radius shapes store `{ radius, height }`
- Polygon shapes store `{ height, points: [{ x, y, z }, ...] }`
- Flags and metadata are serialized as JSON dictionaries

## Custom Storage Backend

To implement a custom storage backend:

1. Implement `IZoneRepository`
2. Pass your implementation to `ZoneManager.Initialize(repository)` and `BlockListManager.Initialize(repository)` before calling `Load()`

This is an advanced use case. Most users should use JSON or MySQL.

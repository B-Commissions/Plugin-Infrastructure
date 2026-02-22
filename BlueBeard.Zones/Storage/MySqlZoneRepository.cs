using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueBeard.Database;
using BlueBeard.Zones.BlockLists;
using BlueBeard.Zones.Storage.Entities;
using Newtonsoft.Json;

namespace BlueBeard.Zones.Storage;

public class MySqlZoneRepository : IZoneRepository
{
    private readonly DbSet<ZoneEntity> _zones;
    private readonly DbSet<BlockListEntity> _blockLists;

    public MySqlZoneRepository(DatabaseManager db)
    {
        db.RegisterEntity<ZoneEntity>();
        db.RegisterEntity<BlockListEntity>();
        db.SyncSchema();
        _zones = db.Table<ZoneEntity>();
        _blockLists = db.Table<BlockListEntity>();
    }

    public async Task<List<ZoneDefinition>> LoadAllAsync()
    {
        var entities = await _zones.QueryAsync();
        return entities.Select(EntityToDefinition).ToList();
    }

    public async Task SaveAsync(ZoneDefinition definition)
    {
        var entity = DefinitionToEntity(definition);
        var existing = await _zones.FirstOrDefaultAsync(z => z.Id == definition.Id);
        if (existing != null)
            await _zones.UpdateAsync(entity);
        else
            await _zones.InsertAsync(entity);
    }

    public async Task DeleteAsync(string id)
    {
        await _zones.DeleteAsync(z => z.Id == id);
    }

    public async Task<List<BlockList>> LoadAllBlockListsAsync()
    {
        var entities = await _blockLists.QueryAsync();
        return entities.Select(EntityToBlockList).ToList();
    }

    public async Task SaveBlockListAsync(BlockList blockList)
    {
        var entity = BlockListToEntity(blockList);
        var existing = await _blockLists.FirstOrDefaultAsync(b => b.Name == blockList.Name);
        if (existing != null)
            await _blockLists.UpdateAsync(entity);
        else
            await _blockLists.InsertAsync(entity);
    }

    public async Task DeleteBlockListAsync(string name)
    {
        await _blockLists.DeleteAsync(b => b.Name == name);
    }

    private static ZoneDefinition EntityToDefinition(ZoneEntity entity)
    {
        var storageData = new ZoneStorageData
        {
            Id = entity.Id,
            CenterX = entity.CenterX,
            CenterY = entity.CenterY,
            CenterZ = entity.CenterZ,
            ShapeType = entity.ShapeType,
            ShapeData = entity.ShapeData,
            FlagsJson = entity.FlagsJson,
            MetadataJson = entity.MetadataJson,
            LowerHeight = entity.LowerHeight == 0 ? null : entity.LowerHeight,
            UpperHeight = entity.UpperHeight == 0 ? null : entity.UpperHeight,
            Priority = entity.Priority
        };
        return ZoneStorageMapper.ToDefinition(storageData);
    }

    private static ZoneEntity DefinitionToEntity(ZoneDefinition definition)
    {
        var data = ZoneStorageMapper.ToStorageData(definition);
        return new ZoneEntity
        {
            Id = data.Id,
            CenterX = data.CenterX,
            CenterY = data.CenterY,
            CenterZ = data.CenterZ,
            ShapeType = data.ShapeType,
            ShapeData = data.ShapeData,
            FlagsJson = data.FlagsJson,
            MetadataJson = data.MetadataJson,
            LowerHeight = data.LowerHeight ?? 0,
            UpperHeight = data.UpperHeight ?? 0,
            Priority = data.Priority
        };
    }

    private static BlockList EntityToBlockList(BlockListEntity entity)
    {
        var items = !string.IsNullOrEmpty(entity.ItemsJson)
            ? JsonConvert.DeserializeObject<HashSet<ushort>>(entity.ItemsJson)
            : new HashSet<ushort>();
        return new BlockList { Name = entity.Name, Items = items };
    }

    private static BlockListEntity BlockListToEntity(BlockList blockList)
    {
        return new BlockListEntity
        {
            Name = blockList.Name,
            ItemsJson = JsonConvert.SerializeObject(blockList.Items)
        };
    }
}

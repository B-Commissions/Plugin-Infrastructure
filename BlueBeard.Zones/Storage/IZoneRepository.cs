using System.Collections.Generic;
using System.Threading.Tasks;
using BlueBeard.Zones.BlockLists;

namespace BlueBeard.Zones.Storage;

public interface IZoneRepository
{
    Task<List<ZoneDefinition>> LoadAllAsync();
    Task SaveAsync(ZoneDefinition definition);
    Task DeleteAsync(string id);
    Task<List<BlockList>> LoadAllBlockListsAsync();
    Task SaveBlockListAsync(BlockList blockList);
    Task DeleteBlockListAsync(string name);
}

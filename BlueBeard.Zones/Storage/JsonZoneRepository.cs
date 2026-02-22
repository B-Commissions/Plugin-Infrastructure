using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueBeard.Zones.BlockLists;
using Newtonsoft.Json;

namespace BlueBeard.Zones.Storage;

public class JsonZoneRepository : IZoneRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonZoneRepository(string pluginDirectory)
    {
        _filePath = Path.Combine(pluginDirectory, "zones.json");
    }

    public async Task<List<ZoneDefinition>> LoadAllAsync()
    {
        var data = await ReadFileAsync();
        return data.Zones.Select(ZoneStorageMapper.ToDefinition).ToList();
    }

    public async Task SaveAsync(ZoneDefinition definition)
    {
        await _lock.WaitAsync();
        try
        {
            var data = await ReadFileUnsafeAsync();
            data.Zones.RemoveAll(z => z.Id == definition.Id);
            data.Zones.Add(ZoneStorageMapper.ToStorageData(definition));
            await WriteFileAsync(data);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var data = await ReadFileUnsafeAsync();
            data.Zones.RemoveAll(z => z.Id == id);
            await WriteFileAsync(data);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<BlockList>> LoadAllBlockListsAsync()
    {
        var data = await ReadFileAsync();
        return data.BlockLists ?? new List<BlockList>();
    }

    public async Task SaveBlockListAsync(BlockList blockList)
    {
        await _lock.WaitAsync();
        try
        {
            var data = await ReadFileUnsafeAsync();
            data.BlockLists.RemoveAll(b => b.Name == blockList.Name);
            data.BlockLists.Add(blockList);
            await WriteFileAsync(data);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteBlockListAsync(string name)
    {
        await _lock.WaitAsync();
        try
        {
            var data = await ReadFileUnsafeAsync();
            data.BlockLists.RemoveAll(b => b.Name == name);
            await WriteFileAsync(data);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<JsonStorageRoot> ReadFileAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await ReadFileUnsafeAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private Task<JsonStorageRoot> ReadFileUnsafeAsync()
    {
        if (!File.Exists(_filePath))
            return Task.FromResult(new JsonStorageRoot());

        var json = File.ReadAllText(_filePath);
        var root = JsonConvert.DeserializeObject<JsonStorageRoot>(json) ?? new JsonStorageRoot();
        return Task.FromResult(root);
    }

    private Task WriteFileAsync(JsonStorageRoot data)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(_filePath, json);
        return Task.CompletedTask;
    }

    private class JsonStorageRoot
    {
        [JsonProperty("zones")]
        public List<ZoneStorageData> Zones { get; set; } = new();

        [JsonProperty("blockLists")]
        public List<BlockList> BlockLists { get; set; } = new();
    }
}

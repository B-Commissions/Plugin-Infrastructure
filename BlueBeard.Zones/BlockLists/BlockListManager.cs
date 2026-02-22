using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueBeard.Core;
using BlueBeard.Core.Helpers;
using BlueBeard.Zones.Storage;
using Rocket.Core.Logging;

namespace BlueBeard.Zones.BlockLists;

public class BlockListManager : IManager
{
    private readonly Dictionary<string, BlockList> _blockLists = new(StringComparer.OrdinalIgnoreCase);
    private IZoneRepository _repository;

    public void Initialize(IZoneRepository repository)
    {
        _repository = repository;
    }

    public void Load()
    {
        if (_repository == null) return;

        ThreadHelper.RunAsynchronously(async () =>
        {
            try
            {
                var lists = await _repository.LoadAllBlockListsAsync();
                ThreadHelper.RunSynchronously(() =>
                {
                    foreach (var list in lists)
                        _blockLists[list.Name] = list;
                    Logger.Log($"[BlueBeard.Zones] Loaded {lists.Count} block list(s) from storage.");
                });
            }
            catch (Exception ex)
            {
                ThreadHelper.RunSynchronously(() =>
                    Logger.LogException(ex, "[BlueBeard.Zones] Failed to load block lists."));
            }
        });
    }

    public void Unload()
    {
        _blockLists.Clear();
    }

    public BlockList GetBlockList(string name)
    {
        _blockLists.TryGetValue(name, out var list);
        return list;
    }

    public IReadOnlyDictionary<string, BlockList> GetAllBlockLists() => _blockLists;

    public async Task CreateBlockListAsync(string name)
    {
        var list = new BlockList { Name = name };
        _blockLists[name] = list;
        if (_repository != null)
            await _repository.SaveBlockListAsync(list);
    }

    public async Task DeleteBlockListAsync(string name)
    {
        _blockLists.Remove(name);
        if (_repository != null)
            await _repository.DeleteBlockListAsync(name);
    }

    public async Task AddItemAsync(string name, ushort itemId)
    {
        if (!_blockLists.TryGetValue(name, out var list)) return;
        list.Items.Add(itemId);
        if (_repository != null)
            await _repository.SaveBlockListAsync(list);
    }

    public async Task RemoveItemAsync(string name, ushort itemId)
    {
        if (!_blockLists.TryGetValue(name, out var list)) return;
        list.Items.Remove(itemId);
        if (_repository != null)
            await _repository.SaveBlockListAsync(list);
    }

    public bool IsItemInBlockList(string name, ushort itemId)
    {
        if (!_blockLists.TryGetValue(name, out var list)) return false;
        return list.Items.Contains(itemId);
    }
}

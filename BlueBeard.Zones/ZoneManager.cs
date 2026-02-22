using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueBeard.Core;
using BlueBeard.Core.Helpers;
using BlueBeard.Zones.Storage;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace BlueBeard.Zones;

public class ZoneManager : IManager
{
    private readonly Dictionary<string, GameObject> _zones = new();
    private readonly Dictionary<string, ZoneDefinition> _definitions = new();
    private IZoneRepository _repository;

    public IReadOnlyDictionary<string, GameObject> Zones => _zones;

    public event Action<Player, ZoneDefinition> PlayerEnteredZone;
    public event Action<Player, ZoneDefinition> PlayerExitedZone;

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
                var definitions = await _repository.LoadAllAsync();
                ThreadHelper.RunSynchronously(() =>
                {
                    foreach (var def in definitions)
                    {
                        CreateZone(def);
                    }
                    Logger.Log($"[BlueBeard.Zones] Loaded {definitions.Count} zone(s) from storage.");
                });
            }
            catch (Exception ex)
            {
                ThreadHelper.RunSynchronously(() =>
                    Logger.LogException(ex, "[BlueBeard.Zones] Failed to load zones from storage."));
            }
        });
    }

    public void Unload()
    {
        foreach (var zone in _zones.Values)
            if (zone != null) UnityEngine.Object.Destroy(zone);
        _zones.Clear();
        _definitions.Clear();
    }

    public void CreateZone(ZoneDefinition definition)
    {
        if (_zones.ContainsKey(definition.Id))
            DestroyZone(definition.Id);

        var go = new GameObject($"Zone_{definition.Id}")
        {
            layer = 19,
            transform = { position = definition.Center }
        };
        UnityEngine.Object.DontDestroyOnLoad(go);
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        definition.Shape.ApplyCollider(go);
        var zone = go.AddComponent<ZoneComponent>();
        zone.Definition = definition;
        zone.PlayerEntered = OnPlayerEntered;
        zone.PlayerExited = OnPlayerExited;
        _zones[definition.Id] = go;
        _definitions[definition.Id] = definition;
    }

    public void DestroyZone(string id)
    {
        if (!_zones.TryGetValue(id, out var go)) return;
        if (go != null) UnityEngine.Object.Destroy(go);
        _zones.Remove(id);
        _definitions.Remove(id);
    }

    public async Task CreateAndSaveZoneAsync(ZoneDefinition definition)
    {
        ThreadHelper.RunSynchronously(() => CreateZone(definition));
        if (_repository != null)
            await _repository.SaveAsync(definition);
    }

    public async Task DestroyAndDeleteZoneAsync(string id)
    {
        ThreadHelper.RunSynchronously(() => DestroyZone(id));
        if (_repository != null)
            await _repository.DeleteAsync(id);
    }

    public async Task SaveZoneAsync(ZoneDefinition definition)
    {
        if (_repository != null)
            await _repository.SaveAsync(definition);
    }

    public ZoneDefinition GetZone(string id)
    {
        _definitions.TryGetValue(id, out var definition);
        return definition;
    }

    public List<ZoneDefinition> GetAllDefinitions()
    {
        return _definitions.Values.ToList();
    }

    private void OnPlayerEntered(Player player, ZoneDefinition definition) => PlayerEnteredZone?.Invoke(player, definition);
    private void OnPlayerExited(Player player, ZoneDefinition definition) => PlayerExitedZone?.Invoke(player, definition);
}

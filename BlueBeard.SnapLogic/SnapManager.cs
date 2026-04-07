using System;
using System.Collections.Generic;
using System.Linq;
using BlueBeard.Core;
using BlueBeard.SnapLogic.Config;
using BlueBeard.SnapLogic.Models;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace BlueBeard.SnapLogic;

/// <summary>
/// Core manager for the snap-point system. Handles barricade event hooks,
/// automatic snap detection, and snap point lifecycle management.
/// </summary>
public class SnapManager : IManager
{
    private readonly Dictionary<string, SnapDefinition> _definitions = new();
    private readonly Dictionary<ushort, string> _assetToDefinition = new();
    private readonly Dictionary<uint, SnapHost> _hosts = new();
    private readonly Dictionary<uint, SnapHost> _childToHost = new();

    private SnapLogicConfig _config;

    /// <summary>
    /// Fired when a barricade is snapped to a point on a host.
    /// </summary>
    public event Action<SnapHost, SnapAttachment> OnItemSnapped;

    /// <summary>
    /// Fired when a snapped barricade is removed from a host.
    /// </summary>
    public event Action<SnapHost, SnapAttachment> OnItemUnsnapped;

    /// <summary>
    /// Fired when a new host barricade is registered.
    /// </summary>
    public event Action<SnapHost> OnHostRegistered;

    /// <summary>
    /// Fired when a host barricade is destroyed or unregistered.
    /// </summary>
    public event Action<SnapHost> OnHostDestroyed;

    /// <summary>
    /// Initializes the manager with optional configuration. If null, defaults are used.
    /// </summary>
    public void Initialize(SnapLogicConfig config = null)
    {
        _config = config ?? new SnapLogicConfig();
        if (config == null)
            _config.LoadDefaults();
    }

    /// <summary>
    /// Registers a snap definition, making a barricade type snap-capable.
    /// </summary>
    public void RegisterDefinition(SnapDefinition definition)
    {
        _definitions[definition.Id] = definition;
        _assetToDefinition[definition.HostAssetId] = definition.Id;
    }

    /// <summary>
    /// Unregisters a snap definition by ID. Existing hosts for this definition are removed.
    /// </summary>
    public void UnregisterDefinition(string id)
    {
        if (!_definitions.TryGetValue(id, out var definition))
            return;

        _assetToDefinition.Remove(definition.HostAssetId);
        _definitions.Remove(id);

        var hostsToRemove = _hosts.Values.Where(h => h.DefinitionId == id).ToList();
        foreach (var host in hostsToRemove)
            RemoveHost(host);
    }

    public void Load()
    {
        if (_config == null)
            Initialize();

        BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
        BarricadeDrop.OnSalvageRequested_Global += OnSalvageRequested;
    }

    public void Unload()
    {
        BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
        BarricadeDrop.OnSalvageRequested_Global -= OnSalvageRequested;

        _hosts.Clear();
        _childToHost.Clear();
    }

    /// <summary>
    /// Gets a snap host by its barricade instance ID.
    /// </summary>
    public SnapHost GetHost(uint hostInstanceId)
    {
        _hosts.TryGetValue(hostInstanceId, out var host);
        return host;
    }

    /// <summary>
    /// Finds the nearest host that has an available snap point accepting the given asset ID.
    /// </summary>
    public SnapHost FindNearestHost(Vector3 position, ushort childAssetId)
    {
        SnapHost nearest = null;
        var nearestDist = float.MaxValue;

        foreach (var host in _hosts.Values)
        {
            if (host.HostDrop?.model == null)
                continue;

            var dist = Vector3.Distance(position, host.HostDrop.model.position);
            if (dist > host.SnapRadius || dist >= nearestDist)
                continue;

            if (host.FindAvailablePoint(childAssetId) != null)
            {
                nearest = host;
                nearestDist = dist;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Returns all active snap hosts.
    /// </summary>
    public IReadOnlyCollection<SnapHost> GetAllHosts() => _hosts.Values;

    /// <summary>
    /// Returns true if the given barricade instance ID is a snapped child.
    /// </summary>
    public bool IsSnapped(uint barricadeInstanceId) => _childToHost.ContainsKey(barricadeInstanceId);

    /// <summary>
    /// Gets the attachment record for a snapped child barricade.
    /// </summary>
    public SnapAttachment GetAttachment(uint childInstanceId)
    {
        if (!_childToHost.TryGetValue(childInstanceId, out var host))
            return null;

        return host.Attachments.Values.FirstOrDefault(a => a.InstanceId == childInstanceId);
    }

    /// <summary>
    /// Attempts to snap a barricade to a specific snap point on a host.
    /// If <paramref name="pointName"/> is null, the nearest available point is used.
    /// </summary>
    /// <returns>The attachment record if successful, null otherwise.</returns>
    public SnapAttachment TrySnap(SnapHost host, BarricadeDrop childDrop, string pointName = null)
    {
        if (host?.HostDrop?.model == null || childDrop?.model == null)
            return null;

        var assetId = childDrop.asset.id;
        SnapPoint point;

        if (pointName != null)
        {
            point = host.GetPoint(pointName);
            if (point == null || host.Attachments.ContainsKey(pointName) || !point.Accepts(assetId))
                return null;
        }
        else
        {
            point = host.FindAvailablePoint(assetId);
            if (point == null)
                return null;
        }

        var hostTransform = host.HostDrop.model;
        var worldPos = hostTransform.TransformPoint(point.PositionOffset);

        BarricadeManager.ServerSetBarricadeTransform(childDrop.model, worldPos, hostTransform.rotation);

        var attachment = new SnapAttachment
        {
            PointName = point.Name,
            AssetId = assetId,
            InstanceId = childDrop.instanceID,
            Drop = childDrop
        };

        host.Attachments[point.Name] = attachment;
        _childToHost[childDrop.instanceID] = host;

        OnItemSnapped?.Invoke(host, attachment);

        return attachment;
    }

    /// <summary>
    /// Removes a snapped child from the given snap point.
    /// </summary>
    /// <returns>True if the attachment was found and removed.</returns>
    public bool Unsnap(SnapHost host, string pointName)
    {
        if (host == null || !host.Attachments.TryGetValue(pointName, out var attachment))
            return false;

        host.Attachments.Remove(pointName);
        _childToHost.Remove(attachment.InstanceId);

        OnItemUnsnapped?.Invoke(host, attachment);
        return true;
    }

    /// <summary>
    /// Removes all snapped children from a host, optionally destroying the barricades.
    /// </summary>
    public void ClearHost(SnapHost host, bool destroyBarricades = false)
    {
        if (host == null)
            return;

        var attachments = host.Attachments.Values.ToList();
        host.Attachments.Clear();

        foreach (var attachment in attachments)
        {
            _childToHost.Remove(attachment.InstanceId);
            OnItemUnsnapped?.Invoke(host, attachment);

            if (destroyBarricades && attachment.Drop?.model != null)
                DestroyBarricade(attachment.Drop);
        }
    }

    /// <summary>
    /// Manually registers a placed barricade as a snap host.
    /// </summary>
    public SnapHost RegisterHost(SnapDefinition definition, BarricadeDrop drop)
    {
        if (definition == null || drop == null)
            return null;

        var data = drop.GetServersideData();

        var host = new SnapHost
        {
            DefinitionId = definition.Id,
            HostInstanceId = drop.instanceID,
            OwnerId = data?.owner ?? 0,
            GroupId = data?.group ?? 0,
            HostDrop = drop,
            SnapPoints = new List<SnapPoint>(definition.SnapPoints),
            SnapRadius = definition.SnapRadius
        };

        _hosts[drop.instanceID] = host;

        OnHostRegistered?.Invoke(host);
        return host;
    }

    private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
    {
        if (drop?.asset == null || drop.model == null)
            return;

        var assetId = drop.asset.id;

        // Check if this is a host barricade
        if (_config.AutoRegisterHosts && _assetToDefinition.TryGetValue(assetId, out var definitionId))
        {
            if (_definitions.TryGetValue(definitionId, out var definition))
            {
                RegisterHost(definition, drop);
                return;
            }
        }

        // Check if this barricade should snap to a nearby host
        var host = FindNearestHost(drop.model.position, assetId);
        if (host != null)
            TrySnap(host, drop);
    }

    private void OnSalvageRequested(BarricadeDrop drop, SteamPlayer steamPlayer, ref bool shouldAllow)
    {
        if (drop == null)
            return;

        var instanceId = drop.instanceID;

        // If this is a snapped child, block salvage
        if (_childToHost.TryGetValue(instanceId, out var parentHost))
        {
            shouldAllow = false;
            return;
        }

        // If this is a host being salvaged, handle cleanup
        if (_hosts.TryGetValue(instanceId, out var host))
        {
            if (_config.DestroyChildrenWithHost)
                ClearHost(host, destroyBarricades: true);
            else
                ClearHost(host, destroyBarricades: false);

            RemoveHost(host);
        }
    }

    private void RemoveHost(SnapHost host)
    {
        _hosts.Remove(host.HostInstanceId);

        foreach (var attachment in host.Attachments.Values)
            _childToHost.Remove(attachment.InstanceId);

        host.Attachments.Clear();

        OnHostDestroyed?.Invoke(host);
    }

    private static void DestroyBarricade(BarricadeDrop drop)
    {
        if (!BarricadeManager.tryGetRegion(drop.model, out var x, out var y, out var plant, out _))
            return;

        BarricadeManager.destroyBarricade(drop, x, y, plant);
    }
}

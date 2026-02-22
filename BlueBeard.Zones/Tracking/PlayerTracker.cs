using System.Collections.Generic;
using System.Linq;
using BlueBeard.Core;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Zones.Tracking;

public class PlayerTracker : IManager
{
    private readonly Dictionary<CSteamID, HashSet<string>> _playerToZones = new();
    private readonly Dictionary<string, HashSet<CSteamID>> _zoneToPlayers = new();
    private ZoneManager _zoneManager;

    public void Initialize(ZoneManager zoneManager)
    {
        _zoneManager = zoneManager;
    }

    public void Load()
    {
        _zoneManager.PlayerEnteredZone += OnPlayerEntered;
        _zoneManager.PlayerExitedZone += OnPlayerExited;
        Provider.onEnemyDisconnected += OnPlayerDisconnected;
    }

    public void Unload()
    {
        _zoneManager.PlayerEnteredZone -= OnPlayerEntered;
        _zoneManager.PlayerExitedZone -= OnPlayerExited;
        Provider.onEnemyDisconnected -= OnPlayerDisconnected;
        _playerToZones.Clear();
        _zoneToPlayers.Clear();
    }

    private void OnPlayerEntered(Player player, ZoneDefinition definition)
    {
        if (!IsWithinHeightBounds(player, definition)) return;

        var steamId = player.channel.owner.playerID.steamID;
        if (!_playerToZones.TryGetValue(steamId, out var zones))
        {
            zones = new HashSet<string>();
            _playerToZones[steamId] = zones;
        }
        zones.Add(definition.Id);

        if (!_zoneToPlayers.TryGetValue(definition.Id, out var players))
        {
            players = new HashSet<CSteamID>();
            _zoneToPlayers[definition.Id] = players;
        }
        players.Add(steamId);
    }

    private void OnPlayerExited(Player player, ZoneDefinition definition)
    {
        var steamId = player.channel.owner.playerID.steamID;

        if (_playerToZones.TryGetValue(steamId, out var zones))
            zones.Remove(definition.Id);

        if (_zoneToPlayers.TryGetValue(definition.Id, out var players))
            players.Remove(steamId);
    }

    private void OnPlayerDisconnected(SteamPlayer steamPlayer)
    {
        var steamId = steamPlayer.playerID.steamID;
        if (!_playerToZones.TryGetValue(steamId, out var zones)) return;

        foreach (var zoneId in zones)
        {
            if (_zoneToPlayers.TryGetValue(zoneId, out var players))
                players.Remove(steamId);
        }
        _playerToZones.Remove(steamId);
    }

    public List<ZoneDefinition> GetZonesForPlayer(Player player)
    {
        var steamId = player.channel.owner.playerID.steamID;
        if (!_playerToZones.TryGetValue(steamId, out var zoneIds))
            return new List<ZoneDefinition>();

        return zoneIds
            .Select(id => _zoneManager.GetZone(id))
            .Where(z => z != null)
            .OrderByDescending(z => z.Priority)
            .ToList();
    }

    public bool IsPlayerInZoneWithFlag(Player player, string flagName, out ZoneDefinition zone, out string flagValue)
    {
        zone = null;
        flagValue = null;

        var zones = GetZonesForPlayer(player);
        foreach (var z in zones)
        {
            if (z.Flags != null && z.Flags.TryGetValue(flagName, out var val))
            {
                zone = z;
                flagValue = val;
                return true;
            }
        }
        return false;
    }

    public List<ZoneDefinition> GetZonesAtPosition(Vector3 position)
    {
        return _zoneManager.GetAllDefinitions()
            .Where(z => IsPositionInZone(position, z))
            .OrderByDescending(z => z.Priority)
            .ToList();
    }

    public bool IsPositionInZoneWithFlag(Vector3 position, string flagName, out ZoneDefinition zone, out string flagValue)
    {
        zone = null;
        flagValue = null;

        var zones = GetZonesAtPosition(position);
        foreach (var z in zones)
        {
            if (z.Flags != null && z.Flags.TryGetValue(flagName, out var val))
            {
                zone = z;
                flagValue = val;
                return true;
            }
        }
        return false;
    }

    private static bool IsWithinHeightBounds(Player player, ZoneDefinition definition)
    {
        if (definition.LowerHeight == null && definition.UpperHeight == null)
            return true;

        var playerY = player.transform.position.y;
        var centerY = definition.Center.y;

        if (definition.LowerHeight.HasValue && playerY < centerY + definition.LowerHeight.Value)
            return false;
        if (definition.UpperHeight.HasValue && playerY > centerY + definition.UpperHeight.Value)
            return false;

        return true;
    }

    private static bool IsPositionInZone(Vector3 position, ZoneDefinition definition)
    {
        if (definition.LowerHeight.HasValue && position.y < definition.Center.y + definition.LowerHeight.Value)
            return false;
        if (definition.UpperHeight.HasValue && position.y > definition.Center.y + definition.UpperHeight.Value)
            return false;

        // Basic distance check for radius zones - the collider handles the real detection
        // This is a rough approximation for position-based queries
        var horizontal = new Vector2(position.x - definition.Center.x, position.z - definition.Center.z);
        if (definition.Shape is Shapes.RadiusZoneShape radius)
            return horizontal.magnitude <= radius.Radius;

        // For polygon zones, we check if the 2D point is inside the polygon
        if (definition.Shape is Shapes.PolygonZoneShape polygon)
            return IsPointInPolygon(position, polygon.WorldPoints);

        return false;
    }

    private static bool IsPointInPolygon(Vector3 point, Vector3[] polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].z > point.z) != (polygon[j].z > point.z) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.z - polygon[i].z) / (polygon[j].z - polygon[i].z) + polygon[i].x)
            {
                inside = !inside;
            }
        }
        return inside;
    }
}

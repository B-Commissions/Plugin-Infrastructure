using System;
using System.Collections.Generic;
using System.Linq;
using BlueBeard.Core;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.Zones.Tracking;

/// <summary>
/// Tracks *effective* zone membership: a player counts as inside a zone only while they
/// are within the trigger collider AND the zone's height band AND (for polygon zones) the
/// true polygon — Unity forces polygon MeshColliders convex, so the collider alone
/// over-triggers on concave shapes.
///
/// Collider enter/exit is the trigger; a lightweight periodic re-check (only for zones
/// with height bounds or polygon shapes) transitions membership as players move
/// vertically or across concave notches, firing <see cref="PlayerEnteredZone"/> /
/// <see cref="PlayerExitedZone"/>. Flag handlers should consume THESE events rather than
/// the raw <see cref="ZoneManager"/> events, which carry no height filtering.
/// </summary>
public class PlayerTracker : IManager
{
    // Zones whose trigger collider the player is currently inside (raw, unfiltered).
    private readonly Dictionary<CSteamID, HashSet<string>> _colliderZones = new();
    // Effective membership (filtered) — what all queries and events are based on.
    private readonly Dictionary<CSteamID, HashSet<string>> _playerToZones = new();
    private readonly Dictionary<string, HashSet<CSteamID>> _zoneToPlayers = new();
    private ZoneManager _zoneManager;
    private GameObject _tickObj;

    /// <summary>Fired when a player's effective membership starts (height/shape filtered).</summary>
    public event Action<Player, ZoneDefinition> PlayerEnteredZone;

    /// <summary>Fired when a player's effective membership ends (exit, height band, concave notch, zone destroyed).</summary>
    public event Action<Player, ZoneDefinition> PlayerExitedZone;

    public void Initialize(ZoneManager zoneManager)
    {
        _zoneManager = zoneManager;
    }

    public void Load()
    {
        _zoneManager.PlayerEnteredZone += OnColliderEntered;
        _zoneManager.PlayerExitedZone += OnColliderExited;
        _zoneManager.ZoneDestroyed += OnZoneDestroyed;
        Provider.onEnemyDisconnected += OnPlayerDisconnected;

        _tickObj = new GameObject("BlueBeard_PlayerTracker_Tick");
        UnityEngine.Object.DontDestroyOnLoad(_tickObj);
        _tickObj.AddComponent<MembershipTicker>().Init(this);
    }

    public void Unload()
    {
        _zoneManager.PlayerEnteredZone -= OnColliderEntered;
        _zoneManager.PlayerExitedZone -= OnColliderExited;
        _zoneManager.ZoneDestroyed -= OnZoneDestroyed;
        Provider.onEnemyDisconnected -= OnPlayerDisconnected;

        if (_tickObj != null) UnityEngine.Object.Destroy(_tickObj);
        _tickObj = null;

        _colliderZones.Clear();
        _playerToZones.Clear();
        _zoneToPlayers.Clear();
    }

    // -----------------------------------------------------------------------
    // Collider events + membership transitions
    // -----------------------------------------------------------------------

    private void OnColliderEntered(Player player, ZoneDefinition definition)
    {
        var steamId = player.channel.owner.playerID.steamID;

        if (!_colliderZones.TryGetValue(steamId, out var raw))
            _colliderZones[steamId] = raw = [];
        raw.Add(definition.Id);

        if (IsEffectivelyInside(player.transform.position, definition))
            AddMembership(player, steamId, definition);
    }

    private void OnColliderExited(Player player, ZoneDefinition definition)
    {
        var steamId = player.channel.owner.playerID.steamID;

        if (_colliderZones.TryGetValue(steamId, out var raw))
            raw.Remove(definition.Id);

        RemoveMembership(player, steamId, definition);
    }

    private void AddMembership(Player player, CSteamID steamId, ZoneDefinition definition)
    {
        if (!_playerToZones.TryGetValue(steamId, out var zones))
            _playerToZones[steamId] = zones = [];
        if (!zones.Add(definition.Id)) return;

        if (!_zoneToPlayers.TryGetValue(definition.Id, out var players))
            _zoneToPlayers[definition.Id] = players = [];
        players.Add(steamId);

        PlayerEnteredZone?.Invoke(player, definition);
    }

    private void RemoveMembership(Player player, CSteamID steamId, ZoneDefinition definition)
    {
        if (!_playerToZones.TryGetValue(steamId, out var zones) || !zones.Remove(definition.Id))
            return;

        if (_zoneToPlayers.TryGetValue(definition.Id, out var players))
            players.Remove(steamId);

        PlayerExitedZone?.Invoke(player, definition);
    }

    private void OnZoneDestroyed(ZoneDefinition definition)
    {
        foreach (var raw in _colliderZones.Values)
            raw.Remove(definition.Id);

        if (!_zoneToPlayers.TryGetValue(definition.Id, out var players)) return;
        _zoneToPlayers.Remove(definition.Id);

        foreach (var steamId in players.ToList())
        {
            if (_playerToZones.TryGetValue(steamId, out var zones))
                zones.Remove(definition.Id);

            var player = PlayerTool.getPlayer(steamId);
            if (player != null)
                PlayerExitedZone?.Invoke(player, definition);
        }
    }

    private void OnPlayerDisconnected(SteamPlayer steamPlayer)
    {
        var steamId = steamPlayer.playerID.steamID;
        _colliderZones.Remove(steamId);

        if (!_playerToZones.TryGetValue(steamId, out var zones)) return;

        foreach (var zoneId in zones)
        {
            if (_zoneToPlayers.TryGetValue(zoneId, out var players))
                players.Remove(steamId);
        }
        _playerToZones.Remove(steamId);
    }

    /// <summary>
    /// Re-evaluate effective membership for players inside colliders of zones that need it
    /// (height-banded or polygon zones). Radius zones without height bounds are fully
    /// handled by collider events and skipped here.
    /// </summary>
    internal void RecheckMemberships()
    {
        foreach (var client in Provider.clients)
        {
            var player = client.player;
            if (player == null) continue;

            var steamId = client.playerID.steamID;
            if (!_colliderZones.TryGetValue(steamId, out var raw) || raw.Count == 0) continue;

            var position = player.transform.position;
            foreach (var zoneId in raw.ToList())
            {
                var definition = _zoneManager.GetZone(zoneId);
                if (definition == null)
                {
                    raw.Remove(zoneId);
                    continue;
                }

                if (!NeedsRecheck(definition)) continue;

                var effective = IsEffectivelyInside(position, definition);
                var isMember = _playerToZones.TryGetValue(steamId, out var zones) && zones.Contains(zoneId);

                if (effective && !isMember) AddMembership(player, steamId, definition);
                else if (!effective && isMember) RemoveMembership(player, steamId, definition);
            }
        }
    }

    private static bool NeedsRecheck(ZoneDefinition definition) =>
        definition.LowerHeight.HasValue || definition.UpperHeight.HasValue ||
        definition.Shape is Shapes.PolygonZoneShape;

    /// <summary>
    /// Height band + true shape test, assuming the position is already inside the trigger
    /// collider. For polygon zones the even-odd polygon test is authoritative — the
    /// collider is only the (convex-hull) trigger.
    /// </summary>
    private static bool IsEffectivelyInside(Vector3 position, ZoneDefinition definition)
    {
        if (!IsWithinHeightBounds(position.y, definition)) return false;
        if (definition.Shape is Shapes.PolygonZoneShape polygon)
            return IsPointInPolygon(position, polygon.WorldPoints);
        return true;
    }

    private sealed class MembershipTicker : MonoBehaviour
    {
        private PlayerTracker _tracker;
        private float _accum;

        public void Init(PlayerTracker tracker) => _tracker = tracker;

        private void Update()
        {
            if (_tracker == null) return;
            _accum += Time.deltaTime;
            if (_accum < 0.5f) return;
            _accum = 0f;
            try { _tracker.RecheckMemberships(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }
    }

    // -----------------------------------------------------------------------
    // Queries
    // -----------------------------------------------------------------------

    public List<ZoneDefinition> GetZonesForPlayer(Player player)
    {
        var steamId = player.channel.owner.playerID.steamID;
        if (!_playerToZones.TryGetValue(steamId, out var zoneIds))
            return [];

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

    // -----------------------------------------------------------------------
    // Pure geometry (public: reusable and unit-testable)
    // -----------------------------------------------------------------------

    public static bool IsWithinHeightBounds(float y, ZoneDefinition definition)
    {
        if (definition.LowerHeight == null && definition.UpperHeight == null)
            return true;

        var centerY = definition.Center.y;
        if (definition.LowerHeight.HasValue && y < centerY + definition.LowerHeight.Value)
            return false;
        if (definition.UpperHeight.HasValue && y > centerY + definition.UpperHeight.Value)
            return false;

        return true;
    }

    public static bool IsPositionInZone(Vector3 position, ZoneDefinition definition)
    {
        if (!IsWithinHeightBounds(position.y, definition))
            return false;

        var horizontal = new Vector2(position.x - definition.Center.x, position.z - definition.Center.z);
        if (definition.Shape is Shapes.RadiusZoneShape radius)
            return horizontal.magnitude <= radius.Radius;

        if (definition.Shape is Shapes.PolygonZoneShape polygon)
            return IsPointInPolygon(position, polygon.WorldPoints);

        return false;
    }

    /// <summary>Standard even-odd ray test on the XZ plane.</summary>
    public static bool IsPointInPolygon(Vector3 point, Vector3[] polygon)
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

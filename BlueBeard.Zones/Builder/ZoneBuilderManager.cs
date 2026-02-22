using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueBeard.Core.Helpers;
using BlueBeard.Zones.Shapes;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.Zones.Builder;

public class ZoneBuilderManager
{
    private readonly Dictionary<ulong, ZoneBuildSession> _sessions = new();
    private ZoneManager _zoneManager;

    public void Initialize(ZoneManager zoneManager)
    {
        _zoneManager = zoneManager;
        Provider.onEnemyDisconnected += OnPlayerDisconnected;
    }

    public void Unload()
    {
        Provider.onEnemyDisconnected -= OnPlayerDisconnected;
        _sessions.Clear();
    }

    public void StartSession(Player player, string zoneId, float height = 30f)
    {
        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        _sessions[steamId] = new ZoneBuildSession
        {
            ZoneId = zoneId,
            Height = height
        };
    }

    public bool AddNode(Player player)
    {
        var session = GetSession(player);
        if (session == null) return false;
        session.Nodes.Add(player.transform.position);
        return true;
    }

    public bool RemoveLastNode(Player player)
    {
        var session = GetSession(player);
        if (session == null || session.Nodes.Count == 0) return false;
        session.Nodes.RemoveAt(session.Nodes.Count - 1);
        return true;
    }

    public async Task<bool> FinishSession(Player player)
    {
        var session = GetSession(player);
        if (session == null || session.Nodes.Count < 3) return false;

        var nodes = session.Nodes.ToArray();
        var center = new Vector3(
            nodes.Average(n => n.x),
            nodes.Average(n => n.y),
            nodes.Average(n => n.z)
        );

        var definition = new ZoneDefinition
        {
            Id = session.ZoneId,
            Center = center,
            Shape = new PolygonZoneShape(nodes, session.Height)
        };

        await _zoneManager.CreateAndSaveZoneAsync(definition);

        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        _sessions.Remove(steamId);
        return true;
    }

    public bool CancelSession(Player player)
    {
        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        return _sessions.Remove(steamId);
    }

    public ZoneBuildSession GetSession(Player player)
    {
        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        _sessions.TryGetValue(steamId, out var session);
        return session;
    }

    public bool HasSession(Player player)
    {
        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        return _sessions.ContainsKey(steamId);
    }

    private void OnPlayerDisconnected(SteamPlayer steamPlayer)
    {
        _sessions.Remove(steamPlayer.playerID.steamID.m_SteamID);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BlueBeard.Core;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace BlueBeard.Holograms;

public class HologramManager : IManager
{
    private class RegistrationState
    {
        public IHologramDisplay Display;
        public List<Hologram> Pool;
        public bool IsGlobal;
        public HashSet<int> GlobalUsedIndices;
        public Func<Player, bool> PlayerFilter;
    }

    private class PlayerState
    {
        public Dictionary<HologramDefinition, int> AssignedIndices = new();
        public Dictionary<HologramDefinition, Dictionary<string, string>> Metadata = new();
        public HashSet<int> UsedIndices = new();
    }

    private readonly Dictionary<HologramDefinition, RegistrationState> _registrations = new();
    private readonly Dictionary<Player, PlayerState> _players = new();
    private readonly List<GameObject> _zones = new();

    public event Action<Player, HologramDefinition> PlayerEnteredHologram;
    public event Action<Player, HologramDefinition> PlayerExitedHologram;

    public IEnumerable<HologramDefinition> GetRegisteredDefinitions() => _registrations.Keys;

    public IEnumerable<(Player Player, HologramDefinition Definition)> GetPlayerAssignments()
    {
        foreach (var kvp in _players)
        foreach (var def in kvp.Value.AssignedIndices.Keys)
            yield return (kvp.Key, def);
    }

    public void Load() { Provider.onEnemyDisconnected += OnPlayerDisconnected; }

    public void Unload()
    {
        Provider.onEnemyDisconnected -= OnPlayerDisconnected;
        foreach (var zone in _zones) { if (zone != null) UnityEngine.Object.Destroy(zone); }
        _zones.Clear();
        _registrations.Clear();
        _players.Clear();
    }

    public void RegisterDefinition(HologramDefinition definition, IHologramDisplay display,
        List<Hologram> pool, bool isGlobal, Func<Player, bool> playerFilter = null)
    {
        RegistrationState state = null;
        foreach (var kvp in _registrations)
        {
            if (ReferenceEquals(kvp.Value.Display, display) && ReferenceEquals(kvp.Value.Pool, pool))
            { state = kvp.Value; break; }
        }
        if (state == null)
        {
            state = new RegistrationState
            {
                Display = display, Pool = pool, IsGlobal = isGlobal,
                GlobalUsedIndices = isGlobal ? new HashSet<int>() : null,
                PlayerFilter = playerFilter
            };
        }
        _registrations[definition] = state;
        CreateZone(definition);
    }

    public void UnregisterDefinition(HologramDefinition definition)
    {
        if (!_registrations.TryGetValue(definition, out var registration)) return;
        foreach (var kvp in _players)
        {
            var player = kvp.Key;
            var playerState = kvp.Value;
            if (!playerState.AssignedIndices.TryGetValue(definition, out var index)) continue;
            var hologram = registration.Pool[index];
            var connection = player.channel.owner.transportConnection;
            registration.Display.Hide(connection, (short)hologram.UI, definition);
            EffectManager.askEffectClearByID(hologram.Effect, connection);
            var usedIndices = registration.IsGlobal ? registration.GlobalUsedIndices : playerState.UsedIndices;
            usedIndices.Remove(index);
            playerState.AssignedIndices.Remove(definition);
            playerState.Metadata.Remove(definition);
        }
        for (var i = _zones.Count - 1; i >= 0; i--)
        {
            var zone = _zones[i];
            if (zone == null) continue;
            var component = zone.GetComponent<HologramZoneComponent>();
            if (component != null && component.Definition == definition)
            { UnityEngine.Object.Destroy(zone); _zones.RemoveAt(i); break; }
        }
        _registrations.Remove(definition);
    }

    public void Register(HologramRegistration registration)
    {
        var state = new RegistrationState
        {
            Display = registration.Display, Pool = registration.Holograms,
            IsGlobal = registration.IsGlobal,
            GlobalUsedIndices = registration.IsGlobal ? new HashSet<int>() : null
        };
        foreach (var definition in registration.Definitions)
        { _registrations[definition] = state; CreateZone(definition); }
    }

    public void UpdatePlayer(Player player, HologramDefinition definition, Dictionary<string, string> metadata)
    {
        if (!_players.TryGetValue(player, out var playerState)) return;
        if (!playerState.AssignedIndices.TryGetValue(definition, out var index)) return;
        if (!_registrations.TryGetValue(definition, out var registration)) return;
        var playerMetadata = playerState.Metadata[definition];
        foreach (var kvp in metadata) playerMetadata[kvp.Key] = kvp.Value;
        var hologram = registration.Pool[index];
        registration.Display.Show(player.channel.owner.transportConnection, (short)hologram.UI, definition, playerMetadata);
    }

    public void UpdateAll(HologramDefinition definition, Dictionary<string, string> metadata)
    {
        if (definition.Metadata != null)
            foreach (var kvp in metadata) definition.Metadata[kvp.Key] = kvp.Value;
        foreach (var kvp in _players)
        {
            if (kvp.Value.AssignedIndices.ContainsKey(definition))
                UpdatePlayer(kvp.Key, definition, metadata);
        }
    }

    private void CreateZone(HologramDefinition definition)
    {
        var go = new GameObject($"HologramZone_{definition.Position}");
        go.layer = 19;
        go.transform.position = definition.Position;
        UnityEngine.Object.DontDestroyOnLoad(go);
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        var collider = go.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.radius = definition.Radius;
        collider.height = definition.Height;
        var zone = go.AddComponent<HologramZoneComponent>();
        zone.Definition = definition;
        zone.PlayerEntered = player => OnPlayerEntered(player, definition);
        zone.PlayerExited = player => OnPlayerExited(player, definition);
        _zones.Add(go);
    }

    private void OnPlayerEntered(Player player, HologramDefinition definition)
    {
        if (!_registrations.TryGetValue(definition, out var registration)) return;
        if (registration.PlayerFilter != null && !registration.PlayerFilter(player)) return;
        if (!_players.TryGetValue(player, out var playerState))
        { playerState = new PlayerState(); _players[player] = playerState; }
        if (playerState.AssignedIndices.ContainsKey(definition)) return;
        var usedIndices = registration.IsGlobal ? registration.GlobalUsedIndices : playerState.UsedIndices;
        var index = -1;
        for (var i = 0; i < registration.Pool.Count; i++)
        { if (!usedIndices.Contains(i)) { index = i; break; } }
        if (index < 0) { Logger.LogWarning($"[HologramManager] Pool exhausted for zone at {definition.Position}"); return; }
        var hologram = registration.Pool[index];
        var playerMetadata = definition.Metadata != null
            ? new Dictionary<string, string>(definition.Metadata) : new Dictionary<string, string>();
        playerState.AssignedIndices[definition] = index;
        playerState.Metadata[definition] = playerMetadata;
        usedIndices.Add(index);
        var connection = player.channel.owner.transportConnection;
        EffectManager.sendEffect(hologram.Effect, connection, definition.Position);
        EffectManager.sendUIEffect(hologram.UI, (short)hologram.UI, connection, true);
        registration.Display.Show(connection, (short)hologram.UI, definition, playerMetadata);
        PlayerEnteredHologram?.Invoke(player, definition);
    }

    private void OnPlayerExited(Player player, HologramDefinition definition)
    {
        if (!_registrations.TryGetValue(definition, out var registration)) return;
        if (!_players.TryGetValue(player, out var playerState)) return;
        if (!playerState.AssignedIndices.TryGetValue(definition, out var index)) return;
        var hologram = registration.Pool[index];
        var connection = player.channel.owner.transportConnection;
        registration.Display.Hide(connection, (short)hologram.UI, definition);
        EffectManager.askEffectClearByID(hologram.Effect, connection);
        var usedIndices = registration.IsGlobal ? registration.GlobalUsedIndices : playerState.UsedIndices;
        usedIndices.Remove(index);
        playerState.AssignedIndices.Remove(definition);
        playerState.Metadata.Remove(definition);
        PlayerExitedHologram?.Invoke(player, definition);
    }

    private void OnPlayerDisconnected(SteamPlayer steamPlayer)
    {
        var player = steamPlayer.player;
        if (!_players.TryGetValue(player, out var playerState)) return;
        foreach (var kvp in playerState.AssignedIndices.ToList())
        {
            if (_registrations.TryGetValue(kvp.Key, out var registration) && registration.IsGlobal)
                registration.GlobalUsedIndices.Remove(kvp.Value);
        }
        _players.Remove(player);
    }
}

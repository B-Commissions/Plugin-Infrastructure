using System;
using System.Collections.Generic;
using BlueBeard.Core;
using BlueBeard.UI;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace BlueBeard.MiniGames;

/// <summary>
/// Lifecycle manager for timed, interactive mini-games. Clients register a handler per
/// mini-game id, then call <see cref="Start"/> to begin a session for a player.
///
/// Starting a mini-game closes any open <see cref="UIManager"/>-managed UI the player
/// currently has active ("mini-game wins" precedence rule) so that EffectManager input
/// events are not double-handled by the UI framework and the mini-game simultaneously.
///
/// Only one mini-game can be active per player at a time. Starting a new one cancels any
/// previous mini-game for that player.
/// </summary>
public class MiniGameManager : IManager
{
    private readonly Dictionary<string, IMiniGameHandler> _handlers = new();
    private readonly Dictionary<ulong, MiniGameTickRunner> _active = new();
    private readonly UIManager _uiManager;
    private GameObject _host;

    /// <summary>Raised after <see cref="IMiniGameHandler.OnEnd"/> fires for any reason.</summary>
    public event Action<MiniGameInstance> MiniGameCompleted;

    /// <summary>
    /// Construct a manager. Pass the plugin's <see cref="UIManager"/> so mini-game Start
    /// can close any open UI for the player before dispatching input events.
    /// </summary>
    public MiniGameManager(UIManager uiManager = null)
    {
        _uiManager = uiManager;
    }

    // -----------------------------------------------------------------------
    // Handler registration
    // -----------------------------------------------------------------------

    public void RegisterHandler(string miniGameId, IMiniGameHandler handler)
    {
        if (string.IsNullOrEmpty(miniGameId)) throw new ArgumentException("miniGameId required", nameof(miniGameId));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        _handlers[miniGameId] = handler;
    }

    public IMiniGameHandler GetHandler(string miniGameId)
    {
        return _handlers.TryGetValue(miniGameId, out var h) ? h : null;
    }

    // -----------------------------------------------------------------------
    // Starting / cancelling / completing
    // -----------------------------------------------------------------------

    /// <summary>
    /// Start a mini-game session for <paramref name="player"/>. Cancels any active
    /// mini-game for the same player, closes any open BlueBeard.UI if a UIManager was
    /// supplied, sends the mini-game effect, and fires <see cref="IMiniGameHandler.OnStart"/>.
    /// </summary>
    public MiniGameInstance Start(Player player, MiniGameDefinition definition)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (!_handlers.TryGetValue(definition.Id, out var handler))
            throw new InvalidOperationException($"No handler registered for mini-game id '{definition.Id}'.");

        var steamId = player.channel.owner.playerID.steamID.m_SteamID;

        // Cancel any existing mini-game for this player.
        if (_active.ContainsKey(steamId))
            Cancel(player);

        // Close any open BlueBeard.UI — mini-game wins input precedence.
        if (_uiManager != null)
        {
            var uPlayer = UnturnedPlayer.FromPlayer(player);
            _uiManager.CloseUI(uPlayer);
        }

        // Send the mini-game effect.
        EffectManager.sendUIEffect(definition.EffectId, (short)definition.EffectId, player.channel.owner.transportConnection, true);

        // Build the instance and the tick runner.
        var instance = new MiniGameInstance(player, definition);
        var runner = _host.AddComponent<MiniGameTickRunner>();
        runner.Instance = instance;
        runner.Handler = handler;
        runner.Manager = this;

        _active[steamId] = runner;

        handler.OnStart(instance);
        return instance;
    }

    /// <summary>
    /// Cancel the player's active mini-game. Calls <see cref="IMiniGameHandler.OnEnd"/>
    /// with the state set to <see cref="MiniGameState.Cancelled"/>, destroys the runner,
    /// and raises <see cref="MiniGameCompleted"/>.
    /// </summary>
    public void Cancel(Player player)
    {
        if (player == null) return;
        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        if (!_active.TryGetValue(steamId, out var runner)) return;

        if (runner.Instance.State == MiniGameState.Running)
            runner.Instance.State = MiniGameState.Cancelled;
        FinalizeRunner(steamId, runner);
    }

    /// <summary>
    /// Called by a handler to conclude a mini-game with a specific result. Sets the state,
    /// destroys the runner, fires <see cref="IMiniGameHandler.OnEnd"/>, then raises
    /// <see cref="MiniGameCompleted"/>.
    /// </summary>
    public void Complete(MiniGameInstance instance, MiniGameState result)
    {
        if (instance == null) return;
        if (instance.State != MiniGameState.Running && result == MiniGameState.TimedOut)
            return; // already finalized; ignore auto-timeout race
        instance.State = result;

        var steamId = instance.Player.channel.owner.playerID.steamID.m_SteamID;
        if (_active.TryGetValue(steamId, out var runner))
            FinalizeRunner(steamId, runner);
    }

    private void FinalizeRunner(ulong steamId, MiniGameTickRunner runner)
    {
        _active.Remove(steamId);

        var instance = runner.Instance;
        var handler = runner.Handler;

        // Clear the mini-game effect so the player's HUD is clean.
        if (instance.Player?.channel?.owner?.transportConnection != null)
        {
            EffectManager.askEffectClearByID(instance.Definition.EffectId, instance.Player.channel.owner.transportConnection);
        }

        UnityEngine.Object.Destroy(runner);

        try { handler.OnEnd(instance); }
        finally { MiniGameCompleted?.Invoke(instance); }
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    public void Load()
    {
        _host = new GameObject("BlueBeard.MiniGames.Host");
        UnityEngine.Object.DontDestroyOnLoad(_host);

        EffectManager.onEffectButtonClicked += OnEffectButtonClicked;
        EffectManager.onEffectTextCommitted += OnEffectTextCommitted;
    }

    public void Unload()
    {
        EffectManager.onEffectButtonClicked -= OnEffectButtonClicked;
        EffectManager.onEffectTextCommitted -= OnEffectTextCommitted;

        // Cancel every still-running instance so handlers' OnEnd fires before shutdown.
        var snapshot = new List<KeyValuePair<ulong, MiniGameTickRunner>>(_active);
        foreach (var kvp in snapshot)
        {
            if (kvp.Value.Instance.State == MiniGameState.Running)
                kvp.Value.Instance.State = MiniGameState.Cancelled;
            FinalizeRunner(kvp.Key, kvp.Value);
        }
        _active.Clear();

        if (_host != null)
        {
            UnityEngine.Object.Destroy(_host);
            _host = null;
        }

        _handlers.Clear();
    }

    // -----------------------------------------------------------------------
    // Input routing
    // -----------------------------------------------------------------------

    private void OnEffectButtonClicked(Player player, string buttonName)
    {
        if (player == null) return;
        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        if (!_active.TryGetValue(steamId, out var runner)) return;
        runner.Handler.OnInput(runner.Instance, buttonName, string.Empty);
    }

    private void OnEffectTextCommitted(Player player, string inputName, string text)
    {
        if (player == null) return;
        var steamId = player.channel.owner.playerID.steamID.m_SteamID;
        if (!_active.TryGetValue(steamId, out var runner)) return;
        runner.Handler.OnInput(runner.Instance, inputName, text ?? string.Empty);
    }
}

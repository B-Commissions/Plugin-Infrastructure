using System;
using System.Collections.Generic;
using BlueBeard.Core;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.UI;

public class UIManager : IManager
{
    // Type-keyed registry of configured UIs.
    private readonly Dictionary<Type, IUI> _uis = new();
    // Per UI type: screen-type -> cached instance.
    private readonly Dictionary<Type, Dictionary<Type, IUIScreen>> _uiScreens = new();
    // Per UI type: default screen type (first AddScreen call, or explicit isDefault).
    private readonly Dictionary<Type, Type> _uiDefaultScreen = new();
    // Per UI type: dialog-type -> cached instance.
    private readonly Dictionary<Type, Dictionary<Type, IUIDialog>> _uiDialogs = new();

    // -----------------------------------------------------------------------
    // Registration
    // -----------------------------------------------------------------------

    /// <summary>
    /// Register a UI type. Instantiates the UI, calls its <see cref="IUI{TSelf}.Configure"/>
    /// hook, and instantiates every screen/dialog type added to the builder. All instances
    /// are cached for the lifetime of the manager.
    /// </summary>
    public void RegisterUI<TUI>() where TUI : IUI<TUI>, new()
    {
        var uiType = typeof(TUI);
        if (_uis.ContainsKey(uiType)) return;

        var ui = new TUI();
        var builder = new UIBuilder();
        ui.Configure(builder);

        var screens = new Dictionary<Type, IUIScreen>();
        foreach (var screenType in builder.ScreenTypes)
        {
            var instance = (IUIScreen)Activator.CreateInstance(screenType);
            screens[screenType] = instance;
        }

        var dialogs = new Dictionary<Type, IUIDialog>();
        foreach (var dialogType in builder.DialogTypes)
        {
            var instance = (IUIDialog)Activator.CreateInstance(dialogType);
            dialogs[dialogType] = instance;
        }

        _uis[uiType] = ui;
        _uiScreens[uiType] = screens;
        _uiDialogs[uiType] = dialogs;
        if (builder.DefaultScreenType != null)
            _uiDefaultScreen[uiType] = builder.DefaultScreenType;
    }

    // -----------------------------------------------------------------------
    // Opening / closing
    // -----------------------------------------------------------------------

    /// <summary>
    /// Open the registered UI of type <typeparamref name="TUI"/> for a player.
    /// Sends the effect, transitions the player to the default screen, and fires
    /// <see cref="IUI.OnOpened"/> + <see cref="IUIScreen.OnShow"/>.
    /// </summary>
    public void OpenUI<TUI>(UnturnedPlayer player) where TUI : IUI<TUI>
    {
        if (!_uis.TryGetValue(typeof(TUI), out var ui))
            return;

        var comp = GetOrAddComponent(player);
        if (comp.IsOpen)
            CloseUI(player);

        EffectManager.sendUIEffect(ui.EffectId, ui.EffectKey, player.Player.channel.owner.transportConnection, true);
        player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);

        comp.CurrentUI = ui;
        comp.IsOpen = true;

        IUIScreen defaultScreen = null;
        if (_uiDefaultScreen.TryGetValue(typeof(TUI), out var defaultType)
            && _uiScreens.TryGetValue(typeof(TUI), out var screens)
            && screens.TryGetValue(defaultType, out defaultScreen))
        {
            comp.CurrentScreen = defaultScreen;
        }

        var context = BuildContext(player, comp);
        ui.OnOpened(context);
        defaultScreen?.OnShow(context);
    }

    public void CloseUI(UnturnedPlayer player)
    {
        var comp = player.Player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp == null || !comp.IsOpen) return;

        var context = BuildContext(player, comp);

        if (comp.CurrentDialog != null)
        {
            comp.CurrentDialog.OnHide(context);
            comp.CurrentDialog = null;
        }

        comp.CurrentScreen?.OnHide(context);
        comp.CurrentUI?.OnClosed(context);

        if (comp.CurrentUI != null)
            EffectManager.askEffectClearByID(comp.CurrentUI.EffectId, player.Player.channel.owner.transportConnection);
        player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);

        comp.Reset();
    }

    // -----------------------------------------------------------------------
    // Screen switching
    // -----------------------------------------------------------------------

    /// <summary>
    /// Transition to a different screen within the currently open UI.
    /// Looks up the cached instance of <typeparamref name="TScreen"/> registered
    /// on the player's active UI.
    /// </summary>
    public void SetScreen<TScreen>(UnturnedPlayer player) where TScreen : IUIScreen
    {
        var comp = player.Player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp == null || !comp.IsOpen || comp.CurrentUI == null) return;

        var uiType = comp.CurrentUI.GetType();
        if (!_uiScreens.TryGetValue(uiType, out var screens)) return;
        if (!screens.TryGetValue(typeof(TScreen), out var screen)) return;

        var context = BuildContext(player, comp);

        if (comp.CurrentDialog != null)
        {
            comp.CurrentDialog.OnHide(context);
            comp.CurrentDialog = null;
        }

        comp.CurrentScreen?.OnHide(context);
        comp.CurrentScreen = screen;
        screen.OnShow(context);
    }

    // -----------------------------------------------------------------------
    // Dialog management
    // -----------------------------------------------------------------------

    public void OpenDialog<TDialog>(UnturnedPlayer player) where TDialog : IUIDialog
    {
        var comp = player.Player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp == null || !comp.IsOpen || comp.CurrentUI == null) return;

        var uiType = comp.CurrentUI.GetType();
        if (!_uiDialogs.TryGetValue(uiType, out var dialogs)) return;
        if (!dialogs.TryGetValue(typeof(TDialog), out var dialog)) return;

        var context = BuildContext(player, comp);

        if (comp.CurrentDialog != null)
            comp.CurrentDialog.OnHide(context);

        comp.CurrentDialog = dialog;
        dialog.OnShow(context);
    }

    public void CloseDialog(UnturnedPlayer player)
    {
        var comp = player.Player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp == null || !comp.IsOpen || comp.CurrentDialog == null) return;

        var context = BuildContext(player, comp);
        comp.CurrentDialog.OnHide(context);
        comp.CurrentDialog = null;
    }

    // -----------------------------------------------------------------------
    // Instance accessors
    // -----------------------------------------------------------------------

    public TUI GetUI<TUI>() where TUI : IUI
    {
        return _uis.TryGetValue(typeof(TUI), out var ui) ? (TUI)ui : default;
    }

    public TScreen GetScreen<TScreen>() where TScreen : IUIScreen
    {
        foreach (var screens in _uiScreens.Values)
            if (screens.TryGetValue(typeof(TScreen), out var screen))
                return (TScreen)screen;
        return default;
    }

    public TDialog GetDialog<TDialog>() where TDialog : IUIDialog
    {
        foreach (var dialogs in _uiDialogs.Values)
            if (dialogs.TryGetValue(typeof(TDialog), out var dialog))
                return (TDialog)dialog;
        return default;
    }

    // -----------------------------------------------------------------------
    // Push updates
    // -----------------------------------------------------------------------

    /// <summary>
    /// Push a keyed update to a player's currently open UI. Dispatch order:
    /// active dialog (if any) -> active screen -> the IUI itself. The first
    /// handler that returns true consumes the update and stops propagation.
    /// Must be called from the main thread.
    /// </summary>
    public void PushUpdate(UnturnedPlayer player, string key, object value)
    {
        var comp = player.Player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp == null || !comp.IsOpen) return;

        var context = BuildContext(player, comp);
        if (comp.CurrentDialog != null && comp.CurrentDialog.OnUpdate(context, key, value))
            return;
        if (comp.CurrentScreen != null && comp.CurrentScreen.OnUpdate(context, key, value))
            return;
        comp.CurrentUI?.OnUpdate(context, key, value);
    }

    /// <summary>
    /// Push an update to every online player who currently has a UI of type
    /// <typeparamref name="TUI"/> open.
    /// </summary>
    public void PushUpdateAll<TUI>(string key, object value) where TUI : IUI
    {
        foreach (var client in Provider.clients)
        {
            if (client?.player == null) continue;
            var comp = client.player.gameObject.GetComponent<UIPlayerComponent>();
            if (comp == null || !comp.IsOpen) continue;
            if (comp.CurrentUI is TUI)
                PushUpdate(UnturnedPlayer.FromPlayer(client.player), key, value);
        }
    }

    /// <summary>
    /// Push an update to every online player whose current screen is of type
    /// <typeparamref name="TScreen"/>.
    /// </summary>
    public void PushUpdateToScreen<TScreen>(string key, object value) where TScreen : IUIScreen
    {
        foreach (var client in Provider.clients)
        {
            if (client?.player == null) continue;
            var comp = client.player.gameObject.GetComponent<UIPlayerComponent>();
            if (comp == null || !comp.IsOpen) continue;
            if (comp.CurrentScreen is TScreen)
                PushUpdate(UnturnedPlayer.FromPlayer(client.player), key, value);
        }
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    public void Load()
    {
        EffectManager.onEffectButtonClicked += OnEffectButtonClicked;
        EffectManager.onEffectTextCommitted += OnEffectTextCommitted;
        Provider.onEnemyDisconnected += OnPlayerDisconnected;
    }

    public void Unload()
    {
        EffectManager.onEffectButtonClicked -= OnEffectButtonClicked;
        EffectManager.onEffectTextCommitted -= OnEffectTextCommitted;
        Provider.onEnemyDisconnected -= OnPlayerDisconnected;

        foreach (var client in Provider.clients)
        {
            if (client.player == null) continue;
            var comp = client.player.gameObject.GetComponent<UIPlayerComponent>();
            if (comp != null && comp.IsOpen)
            {
                var uPlayer = UnturnedPlayer.FromPlayer(client.player);
                CloseUI(uPlayer);
            }
        }

        _uis.Clear();
        _uiScreens.Clear();
        _uiDefaultScreen.Clear();
        _uiDialogs.Clear();
    }

    // -----------------------------------------------------------------------
    // Event routing
    // -----------------------------------------------------------------------

    private void OnEffectButtonClicked(Player player, string buttonName)
    {
        var comp = player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp == null || !comp.IsOpen || comp.CurrentUI == null) return;

        var context = BuildContext(UnturnedPlayer.FromPlayer(player), comp);
        comp.CurrentUI.OnButtonPressed(context, buttonName);
    }

    private void OnEffectTextCommitted(Player player, string inputName, string text)
    {
        var comp = player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp == null || !comp.IsOpen || comp.CurrentUI == null) return;

        var context = BuildContext(UnturnedPlayer.FromPlayer(player), comp);
        comp.CurrentUI.OnTextSubmitted(context, inputName, text);
    }

    private void OnPlayerDisconnected(SteamPlayer steamPlayer)
    {
        if (steamPlayer.player == null) return;
        var comp = steamPlayer.player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp != null && comp.IsOpen)
            comp.Reset();
    }

    private static UIPlayerComponent GetOrAddComponent(UnturnedPlayer player)
    {
        var comp = player.Player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp == null)
            comp = player.Player.gameObject.AddComponent<UIPlayerComponent>();
        return comp;
    }

    private static UIContext BuildContext(UnturnedPlayer player, UIPlayerComponent comp)
    {
        ITransportConnection connection = player.Player.channel.owner.transportConnection;
        short effectKey = comp.CurrentUI?.EffectKey ?? 0;
        return new UIContext(player, connection, effectKey, comp);
    }
}

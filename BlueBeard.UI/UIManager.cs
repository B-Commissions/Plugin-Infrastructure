using System.Collections.Generic;
using BlueBeard.Core;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using UnityEngine;

namespace BlueBeard.UI;

public class UIManager : IManager
{
    private readonly Dictionary<string, IUI> _uis = new();

    public void RegisterUI(IUI ui)
    {
        _uis[ui.Id] = ui;
    }

    public void OpenUI(UnturnedPlayer player, IUI ui)
    {
        var comp = GetOrAddComponent(player);

        if (comp.IsOpen)
            CloseUI(player);

        EffectManager.sendUIEffect(ui.EffectId, ui.EffectKey, player.Player.channel.owner.transportConnection, true);
        player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);

        comp.CurrentUI = ui;
        comp.CurrentScreen = ui.DefaultScreen;
        comp.IsOpen = true;

        var context = BuildContext(player, comp);
        ui.OnOpened(context);
        ui.DefaultScreen?.OnShow(context);
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

        EffectManager.askEffectClearByID(comp.CurrentUI.EffectId, player.Player.channel.owner.transportConnection);
        player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);

        comp.Reset();
    }

    public void SetScreen(UnturnedPlayer player, IUIScreen screen)
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
        comp.CurrentScreen = screen;
        screen.OnShow(context);
    }

    public void OpenDialog(UnturnedPlayer player, IUIDialog dialog)
    {
        var comp = player.Player.gameObject.GetComponent<UIPlayerComponent>();
        if (comp == null || !comp.IsOpen) return;

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
    }

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

# Event Routing

This page explains how button clicks and text submissions travel from Unturned's `EffectManager` to your IUI, IUIScreen, and IUIDialog implementations.

---

## How UIManager Hooks Events

When `UIManager.Load()` is called, it subscribes to two global Unturned events:

```csharp
EffectManager.onEffectButtonClicked += OnEffectButtonClicked;
EffectManager.onEffectTextCommitted += OnEffectTextCommitted;
```

These events fire for **every** UI effect on the server, not just BlueBeard.UI effects. UIManager filters by checking whether the player has an active `UIPlayerComponent`.

When `UIManager.Unload()` is called, both subscriptions are removed.

---

## Event Dispatch Flow

When a player interacts with a UI element, the following sequence occurs:

### Step 1: Unturned fires the event

```
EffectManager.onEffectButtonClicked(Player player, string buttonName)
EffectManager.onEffectTextCommitted(Player player, string inputName, string text)
```

These are native Unturned delegates. The `Player` is the SDG player object, and `buttonName`/`inputName` is the name of the Unity GameObject that was clicked or submitted.

### Step 2: UIManager looks up the player's component

```csharp
var comp = player.gameObject.GetComponent<UIPlayerComponent>();
if (comp == null || !comp.IsOpen || comp.CurrentUI == null) return;
```

If the player does not have a `UIPlayerComponent`, or if `IsOpen` is `false`, or if `CurrentUI` is `null`, the event is **silently ignored**. This means events from non-BlueBeard UIs or from players who do not have a BlueBeard UI open will never cause errors.

### Step 3: UIManager builds a UIContext and calls the IUI

```csharp
var context = BuildContext(UnturnedPlayer.FromPlayer(player), comp);
comp.CurrentUI.OnButtonPressed(context, buttonName);
// or
comp.CurrentUI.OnTextSubmitted(context, inputName, text);
```

UIManager always delivers the event to the **IUI** level. It does not route to screens or dialogs directly. The IUI implementation is responsible for all further routing.

### Step 4: IUI routes to the correct handler

Inside your `IUI.OnButtonPressed` / `IUI.OnTextSubmitted`, you decide where the event goes:

1. **Handle global buttons first** -- close button, tab switches, or any button that is always visible regardless of which screen or dialog is active.
2. **If a dialog is open, route to the dialog** -- check `ctx.Component.CurrentDialog != null` and call its `OnButtonPressed` / `OnTextSubmitted`.
3. **Otherwise, route to the current screen** -- call `ctx.Component.CurrentScreen.OnButtonPressed` / `OnTextSubmitted`.

---

## Complete Routing Example

Here is a full example showing how a FactionUI routes both button and text events:

```csharp
using BlueBeard.UI;

public class FactionUI : IUI
{
    public string Id => "faction";
    public ushort EffectId => 50600;
    public short EffectKey => (short)EffectId;
    public IUIScreen[] Screens => new IUIScreen[] { OverviewScreen, MembersScreen };
    public IUIScreen DefaultScreen => OverviewScreen;

    public FactionOverviewScreen OverviewScreen { get; } = new();
    public FactionMembersScreen MembersScreen { get; } = new();

    private UIManager _uiManager;

    public FactionUI(UIManager uiManager)
    {
        _uiManager = uiManager;
    }

    public void OnOpened(UIContext ctx)
    {
        // Populate header data visible on all screens
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/Header/FactionName", "The Iron Guard");
    }

    public void OnClosed(UIContext ctx)
    {
        // Nothing to clean up in this example
    }

    public void OnButtonPressed(UIContext ctx, string buttonName)
    {
        // ---- 1. Global buttons ----
        switch (buttonName)
        {
            case "Faction_Close":
                _uiManager.CloseUI(ctx.Player);
                return;

            case "Faction_Tab_Overview":
                _uiManager.SetScreen(ctx.Player, OverviewScreen);
                return;

            case "Faction_Tab_Members":
                _uiManager.SetScreen(ctx.Player, MembersScreen);
                return;
        }

        // ---- 2. Dialog takes priority ----
        if (ctx.Component.CurrentDialog != null)
        {
            ctx.Component.CurrentDialog.OnButtonPressed(ctx, buttonName);
            return;
        }

        // ---- 3. Route to current screen ----
        ctx.Component.CurrentScreen?.OnButtonPressed(ctx, buttonName);
    }

    public void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        // Dialogs first, then screen
        if (ctx.Component.CurrentDialog != null)
        {
            ctx.Component.CurrentDialog.OnTextSubmitted(ctx, inputName, text);
            return;
        }

        ctx.Component.CurrentScreen?.OnTextSubmitted(ctx, inputName, text);
    }
}
```

### What happens when a player clicks "Members_NextPage"

1. Unturned fires `onEffectButtonClicked(player, "Members_NextPage")`.
2. UIManager finds the player's `UIPlayerComponent`. `IsOpen` is `true` and `CurrentUI` is `FactionUI`.
3. UIManager calls `FactionUI.OnButtonPressed(context, "Members_NextPage")`.
4. `FactionUI` checks global buttons -- `"Members_NextPage"` does not match any.
5. `CurrentDialog` is `null` (no dialog is open).
6. `FactionUI` calls `MembersScreen.OnButtonPressed(context, "Members_NextPage")`.
7. `MembersScreen` increments the page in `State` and refreshes the list.

### What happens when a player clicks "ConfirmKick_Yes" (dialog is open)

1. Unturned fires `onEffectButtonClicked(player, "ConfirmKick_Yes")`.
2. UIManager calls `FactionUI.OnButtonPressed(context, "ConfirmKick_Yes")`.
3. `FactionUI` checks global buttons -- no match.
4. `CurrentDialog` is `ConfirmKickDialog` (not `null`).
5. `FactionUI` calls `ConfirmKickDialog.OnButtonPressed(context, "ConfirmKick_Yes")`.
6. The dialog performs the kick action and calls `_uiManager.CloseDialog(ctx.Player)`.

---

## Important Notes

- **Button names are global within an effect.** Unturned does not scope button names by screen or dialog. Use a naming convention that includes the screen/dialog prefix (e.g., `Members_NextPage`, `ConfirmKick_Yes`) to avoid collisions.
- **UIManager does not filter by effect key.** If a player has a BlueBeard UI open and clicks a button in a completely different UI effect (from another plugin), UIManager will still deliver the event to `IUI.OnButtonPressed`. Your button-name checks will naturally ignore unknown button names, so this is harmless in practice, but be aware of it.
- **Events are synchronous.** `OnButtonPressed` and `OnTextSubmitted` run on the main thread. If you need to do async work (database queries, HTTP requests), use `ThreadHelper.RunAsynchronously` and update the UI from within `ThreadHelper.RunSynchronously`.

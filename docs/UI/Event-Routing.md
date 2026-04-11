# Event Routing

This page explains how button clicks and text submissions travel from Unturned's `EffectManager` to your `IUI`, `IUIScreen`, and `IUIDialog` implementations, plus the separate push-update dispatch chain.

---

## How UIManager Hooks Events

When `UIManager.Load()` is called, it subscribes to two global Unturned events:

```csharp
EffectManager.onEffectButtonClicked += OnEffectButtonClicked;
EffectManager.onEffectTextCommitted += OnEffectTextCommitted;
```

These events fire for **every** UI effect on the server, not just BlueBeard.UI effects. UIManager filters by checking whether the player has an active `UIPlayerComponent` with `IsOpen == true`.

When `UIManager.Unload()` is called, both subscriptions are removed.

---

## Event Dispatch Flow

### Step 1: Unturned fires the event

```
EffectManager.onEffectButtonClicked(Player player, string buttonName)
EffectManager.onEffectTextCommitted(Player player, string inputName, string text)
```

These are native Unturned delegates. `Player` is the SDG player object, and `buttonName` / `inputName` is the name of the Unity GameObject that was clicked or submitted.

### Step 2: UIManager looks up the player's component

```csharp
var comp = player.gameObject.GetComponent<UIPlayerComponent>();
if (comp == null || !comp.IsOpen || comp.CurrentUI == null) return;
```

If the player does not have a `UIPlayerComponent`, or if `IsOpen` is `false`, or if `CurrentUI` is `null`, the event is silently ignored.

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

1. **Handle global buttons first** -- close button, tab switches, or any button that is always visible regardless of which screen or dialog is active. Navigate via `SetScreen<T>()` / `CloseUI()`.
2. **If a dialog is open, route to the dialog** -- check `ctx.Component.CurrentDialog != null` and call its `OnButtonPressed` / `OnTextSubmitted`.
3. **Otherwise, route to the current screen** -- call `ctx.Component.CurrentScreen.OnButtonPressed` / `OnTextSubmitted`.

---

## Complete Routing Example

```csharp
using BlueBeard.UI;

public class FactionUI : UIBase, IUI<FactionUI>
{
    public override string Id => "faction";
    public override ushort EffectId => 50600;
    public override short EffectKey => (short)EffectId;

    public void Configure(UIBuilder builder)
    {
        builder
            .AddScreen<FactionOverviewScreen>(isDefault: true)
            .AddScreen<FactionMembersScreen>()
            .AddDialog<ConfirmKickDialog>();
    }

    public override void OnOpened(UIContext ctx)
    {
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/Header/FactionName", "The Iron Guard");
    }

    public override void OnButtonPressed(UIContext ctx, string buttonName)
    {
        // ---- 1. Global buttons ----
        switch (buttonName)
        {
            case "Faction_Close":
                MyPlugin.UI.CloseUI(ctx.Player);
                return;

            case "Faction_Tab_Overview":
                MyPlugin.UI.SetScreen<FactionOverviewScreen>(ctx.Player);
                return;

            case "Faction_Tab_Members":
                MyPlugin.UI.SetScreen<FactionMembersScreen>(ctx.Player);
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

    public override void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        if (ctx.Component.CurrentDialog != null)
        {
            ctx.Component.CurrentDialog.OnTextSubmitted(ctx, inputName, text);
            return;
        }

        ctx.Component.CurrentScreen?.OnTextSubmitted(ctx, inputName, text);
    }
}
```

### Opening a dialog from a screen

```csharp
public class FactionMembersScreen : UIScreenBase
{
    public override string Id => "members";

    public override void OnButtonPressed(UIContext ctx, string buttonName)
    {
        if (buttonName.StartsWith("Kick_"))
        {
            var targetIdStr = buttonName.Substring(5);
            ctx.Component.State["kick.targetId"] = ulong.Parse(targetIdStr);
            MyPlugin.UI.OpenDialog<ConfirmKickDialog>(ctx.Player);
        }
    }
}
```

The dialog is looked up by type from the cached set registered on this UI.

### What happens when a player clicks "Members_NextPage"

1. Unturned fires `onEffectButtonClicked(player, "Members_NextPage")`.
2. UIManager finds the player's `UIPlayerComponent`. `IsOpen` is `true` and `CurrentUI` is the cached `FactionUI` instance.
3. UIManager calls `FactionUI.OnButtonPressed(context, "Members_NextPage")`.
4. `FactionUI` checks global buttons -- no match.
5. `CurrentDialog` is `null`.
6. `FactionUI` calls `MembersScreen.OnButtonPressed(context, "Members_NextPage")`.
7. `MembersScreen` increments the page in `ctx.Component.State` and refreshes the list.

### What happens when a player clicks "ConfirmKick_Yes" (dialog open)

1. Unturned fires `onEffectButtonClicked(player, "ConfirmKick_Yes")`.
2. UIManager calls `FactionUI.OnButtonPressed(context, "ConfirmKick_Yes")`.
3. `FactionUI` checks global buttons -- no match.
4. `CurrentDialog` is the cached `ConfirmKickDialog`.
5. `FactionUI` calls `ConfirmKickDialog.OnButtonPressed(context, "ConfirmKick_Yes")`.
6. The dialog performs the kick action and calls `MyPlugin.UI.CloseDialog(ctx.Player)`.

---

## Push Update Dispatch

Push updates use a separate entry point (`UIManager.PushUpdate` / `PushUpdateAll<TUI>` / `PushUpdateToScreen<TScreen>`) and a different dispatch chain than button/text events.

### The chain

```
PushUpdate(player, key, value)
  |
  +-- CurrentDialog != null ?
  |     +-- CurrentDialog.OnUpdate(ctx, key, value)
  |         +-- returned true?  -> stop
  |         +-- returned false? -> continue
  |
  +-- CurrentScreen != null ?
  |     +-- CurrentScreen.OnUpdate(ctx, key, value)
  |         +-- returned true?  -> stop
  |         +-- returned false? -> continue
  |
  +-- CurrentUI.OnUpdate(ctx, key, value)
```

The dispatch goes **dialog first, then screen, then IUI**. This mirrors the priority that button/text events have in the routing examples above -- the most specific handler sees the update first, and any layer can consume it.

### Returning true vs false

- `return true` = "I handled this update". No further layers are called.
- `return false` = "Not mine". The chain continues to the next layer.

The default (inherited from `UIBase` / `UIScreenBase` / `UIDialogBase`) is `return false`, so unhandled updates naturally propagate upward.

### Example: rent payment notification

A `RentManager` external to the UI framework wants to notify the currently-open `HousingUI` that a payment was collected:

```csharp
// Inside RentManager after collecting rent (main thread):
MyPlugin.UI.PushUpdate(ownerPlayer, "rent.collected", new Dictionary<string, object>
{
    ["renter"]   = renterName,
    ["amount"]   = property.RentPrice,
    ["next_due"] = property.RentDueAt,
});
```

The `ManagementScreen` handles it only when the player is currently on the management tab:

```csharp
public class ManagementScreen : UIScreenBase
{
    public override string Id => "management";

    public override bool OnUpdate(UIContext ctx, string key, object value)
    {
        if (key != "rent.collected") return false;

        var data = (Dictionary<string, object>)value;
        var amount = (int)data["amount"];
        var renter = (string)data["renter"];

        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/ManagementPanel/RentStatus",
            $"Last payment: {amount} EXP from {renter}");

        return true;
    }
}
```

If the player is on a different tab, `ManagementScreen.OnUpdate` is never called -- the chain passes through the currently active screen. The rent manager can call `PushUpdateToScreen<ManagementScreen>` if it specifically wants to hit viewers of that tab across all online players.

### Broadcast updates

```csharp
// Everyone viewing the ShopUI sees a stock update:
MyPlugin.UI.PushUpdateAll<ShopUI>("stock.changed", new { ItemId = 1234, Remaining = 5 });

// Everyone on the management tab sees the rent notice:
MyPlugin.UI.PushUpdateToScreen<ManagementScreen>("rent.collected", payload);
```

Both iterate `Provider.clients`, skip players without an open UI, and apply the same dialog→screen→UI dispatch chain to each matching player.

### Thread safety

`PushUpdate` (and its broadcast variants) must be called from the main thread -- they invoke `EffectManager` methods inside the handler. If dispatching from a background worker:

```csharp
ThreadHelper.RunSynchronously(() =>
{
    MyPlugin.UI.PushUpdate(player, "rent.collected", payload);
});
```

### Key naming convention

Use dot-separated domain prefixes to avoid collisions, the same convention the `UIPlayerComponent.State` dictionary uses:

- `rent.collected`, `rent.evicted`, `rent.due_warning`
- `property.reclaimed`, `property.owner_changed`
- `stock.changed`, `stock.depleted`

---

## Important Notes

- **Button names are global within an effect.** Unturned does not scope button names by screen or dialog. Use a naming convention that includes the screen/dialog prefix (e.g., `Members_NextPage`, `ConfirmKick_Yes`) to avoid collisions.
- **UIManager does not filter by effect key.** If a player has a BlueBeard UI open and clicks a button in a completely different UI effect (from another plugin), UIManager will still deliver the event to `IUI.OnButtonPressed`. Your button-name checks will naturally ignore unknown button names, so this is harmless in practice.
- **Events are synchronous.** `OnButtonPressed`, `OnTextSubmitted`, and `OnUpdate` all run on the main thread. If you need to do async work (database queries, HTTP requests), use `ThreadHelper.RunAsynchronously` and marshal UI updates back through `ThreadHelper.RunSynchronously`.
- **Mini-games intercept input before the UI.** If a player has an active `BlueBeard.MiniGames` instance, the mini-game is started *after* any open UI has been closed (that's `MiniGameManager.Start`'s precedence rule). During the mini-game, `EffectManager` button clicks are routed to the mini-game handler, not to the UI.

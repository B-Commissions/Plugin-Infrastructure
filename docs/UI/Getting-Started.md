# Getting Started

## Project References

Add references to both BlueBeard.UI and BlueBeard.Core in your `.csproj`:

```xml
<ProjectReference Include="..\BlueBeard.UI\BlueBeard.UI.csproj" />
<ProjectReference Include="..\BlueBeard.Core\BlueBeard.Core.csproj" />
```

BlueBeard.Core is required because UIManager implements `IManager` (the `Load`/`Unload` lifecycle interface) and because you will likely use helpers such as `ThreadHelper` for async data loading.

---

## Architecture

BlueBeard.UI uses a three-level hierarchy:

```
IUI  (top-level container, e.g. FactionUI)
 +-- IUIScreen  (a page or tab, e.g. OverviewScreen, MembersScreen)
 +-- IUIDialog  (a modal popup, e.g. ConfirmKickDialog)
```

- **IUI** is the top-level container. Every registered UI implements `IUI<TSelf>` and declares its screens and dialogs in `Configure(UIBuilder)`.
- **IUIScreen** represents a full page of content. Only one screen is active at a time.
- **IUIDialog** is a popup overlay shown on top of a screen. Only one dialog can be open at a time. Dialogs are registered at the UI level (not per-screen) and any screen can open any dialog.

All three levels live under their parent `IUI`. Screens and dialogs are instantiated once during `RegisterUI` and cached for the lifetime of the UIManager.

---

## Declaring a UI

Implement `IUI<TSelf>` and declare screens/dialogs in `Configure`:

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
            .AddDialog<ConfirmKickDialog>()
            .AddDialog<ConfirmRentDialog>();
    }

    public override void OnButtonPressed(UIContext ctx, string buttonName)
    {
        // Route tab-switching buttons directly; fall through to dialog/screen otherwise.
        switch (buttonName)
        {
            case "Faction_Close":         MyPlugin.UI.CloseUI(ctx.Player); return;
            case "Faction_Tab_Overview":  MyPlugin.UI.SetScreen<FactionOverviewScreen>(ctx.Player); return;
            case "Faction_Tab_Members":   MyPlugin.UI.SetScreen<FactionMembersScreen>(ctx.Player); return;
        }

        if (ctx.Component.CurrentDialog != null)
            ctx.Component.CurrentDialog.OnButtonPressed(ctx, buttonName);
        else
            ctx.Component.CurrentScreen?.OnButtonPressed(ctx, buttonName);
    }
}
```

Screens and dialogs inherit `UIScreenBase` / `UIDialogBase`:

```csharp
public class FactionOverviewScreen : UIScreenBase
{
    public override string Id => "overview";

    public override void OnShow(UIContext ctx)
    {
        // Populate text fields, etc.
    }
}

public class ConfirmKickDialog : UIDialogBase
{
    public override string Id => "confirm_kick";

    public override void OnButtonPressed(UIContext ctx, string buttonName)
    {
        if (buttonName == "Confirm_Yes") { /* kick */ MyPlugin.UI.CloseDialog(ctx.Player); }
        if (buttonName == "Confirm_No")  { MyPlugin.UI.CloseDialog(ctx.Player); }
    }
}
```

Every screen and dialog must have a parameterless constructor (the `new()` constraint on `AddScreen<T>` / `AddDialog<T>`). Access plugin dependencies through a static singleton on your plugin class.

---

## UIManager API

`UIManager` is the central coordinator. It implements `IManager`, so you call `Load()` during plugin startup and `Unload()` during plugin shutdown.

| Method | Description |
|--------|-------------|
| `RegisterUI<TUI>()` | Instantiate `TUI`, call its `Configure`, and cache one instance of every screen and dialog it declares. |
| `OpenUI<TUI>(UnturnedPlayer)` | Send the UI effect to the player, enable the modal cursor, transition to the default screen, and fire `OnOpened` + `OnShow`. If the player already has a UI open, it is closed first. |
| `CloseUI(UnturnedPlayer)` | Hide the active dialog (if any), hide the current screen, call `OnClosed`, clear the effect, disable the modal cursor, and reset the player component. |
| `SetScreen<TScreen>(UnturnedPlayer)` | Switch screens within the active UI. Closes any open dialog, calls `OnHide` on the old screen, sets the new screen, and calls `OnShow`. |
| `OpenDialog<TDialog>(UnturnedPlayer)` | Open a dialog registered on the active UI. If another dialog is already open it is closed first. |
| `CloseDialog(UnturnedPlayer)` | Close the active dialog and call its `OnHide`. |
| `GetUI<TUI>() / GetScreen<TScreen>() / GetDialog<TDialog>()` | Retrieve the cached instance by type (e.g. for reading state). |
| `PushUpdate(player, key, value)` | Push a keyed update into the player's currently open UI. Dispatch order: dialog → screen → IUI, first to return `true` consumes it. |
| `PushUpdateAll<TUI>(key, value)` | Push to every online player whose active UI is of type `TUI`. |
| `PushUpdateToScreen<TScreen>(key, value)` | Push to every online player whose active screen is of type `TScreen`. |

### Initialization

```csharp
// In your plugin's Load() or OnLevelLoaded:
var uiManager = new UIManager();
uiManager.Load();

// Register your UIs by type:
uiManager.RegisterUI<FactionUI>();
uiManager.RegisterUI<ShopUI>();

// Open for a specific player:
uiManager.OpenUI<FactionUI>(player);

// In your plugin's Unload():
uiManager.Unload();
```

---

## Event Flow

When a player clicks a button or submits text in an Unturned UI effect:

```
EffectManager.onEffectButtonClicked / onEffectTextCommitted
  |
  v
UIManager  (looks up the player's UIPlayerComponent)
  |  -- if no UI is open, event is silently ignored
  v
IUI.OnButtonPressed(context, buttonName)
  |
  v
Your IUI implementation routes to:
  1. Global buttons (close, tab switches) via SetScreen<T> / CloseUI
  2. CurrentDialog.OnButtonPressed (if a dialog is open)
  3. CurrentScreen.OnButtonPressed (otherwise)
```

UIManager only calls the IUI layer. The IUI implementation is responsible for routing further to the correct screen or dialog. See [Event Routing](Event-Routing.md).

---

## Push Updates

External managers often need to notify a player's UI that something changed -- a rent payment was collected, stock count changed, a timer expired. Instead of holding direct references to UI internals, call `PushUpdate`:

```csharp
// Inside RentManager after collecting rent:
MyPlugin.UI.PushUpdate(ownerPlayer, "rent.collected", new Dictionary<string, object>
{
    ["renter"] = renterName,
    ["amount"] = property.RentPrice,
});
```

The active dialog sees the update first, then the active screen, then the IUI itself. Any of them can consume the update by returning `true` from `OnUpdate`:

```csharp
public class ManagementScreen : UIScreenBase
{
    public override string Id => "management";

    public override bool OnUpdate(UIContext ctx, string key, object value)
    {
        if (key != "rent.collected") return false;
        var data = (Dictionary<string, object>)value;
        var amount = (int)data["amount"];
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/RentStatus", $"Last payment: {amount} EXP");
        return true;
    }
}
```

For a broadcast update to every online player viewing a specific UI type:

```csharp
MyPlugin.UI.PushUpdateAll<ShopUI>("stock.changed", new { ItemId = 1234, Remaining = 5 });
```

Push updates must be called from the main thread (they invoke `EffectManager`). If you're dispatching from a background worker, wrap the call in `ThreadHelper.RunSynchronously`.

See [Event Routing](Event-Routing.md) for the full dispatch sequence.

---

## Lifecycle

### Opening a UI

```
OpenUI<TUI>(player)
  1. If the player already has a UI open, CloseUI is called first
  2. EffectManager.sendUIEffect(effectId, effectKey, connection, true)
  3. Player modal cursor is enabled (EPluginWidgetFlags.Modal)
  4. UIPlayerComponent is initialized: CurrentUI = ui, CurrentScreen = default, IsOpen = true
  5. ui.OnOpened(context)
  6. defaultScreen.OnShow(context)
```

### Switching Screens

```
SetScreen<TScreen>(player)
  1. Resolve the cached instance of TScreen registered on the active UI
  2. If a dialog is open, dialog.OnHide(context) is called and CurrentDialog is set to null
  3. CurrentScreen.OnHide(context)
  4. CurrentScreen is set to the new screen instance
  5. newScreen.OnShow(context)
```

### Opening a Dialog

```
OpenDialog<TDialog>(player)
  1. Resolve the cached instance of TDialog registered on the active UI
  2. If another dialog is already open, its OnHide(context) is called
  3. CurrentDialog is set to the new dialog instance
  4. dialog.OnShow(context)
```

### Automatic Cleanup

UIManager hooks two events for automatic cleanup:

- **Player disconnect** (`Provider.onEnemyDisconnected`): the player's `UIPlayerComponent` is reset. No `OnHide`/`OnClosed` callbacks are fired because the player is already gone.
- **Plugin unload** (`Unload()`): iterates every online player, and if they have an open UI, calls `CloseUI` on them (which fires the full close lifecycle). Then clears all registered UIs and unhooks events.

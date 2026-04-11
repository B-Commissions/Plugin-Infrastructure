# BlueBeard.UI

A reusable framework for building full-screen UIs using Unturned's `EffectManager` API. Provides a type-driven `IUI` / `IUIScreen` / `IUIDialog` hierarchy with automatic event routing, per-player state, modal management, a push-update dispatch system, and cleanup.

## Installation

```xml
<ProjectReference Include="..\BlueBeard.UI\BlueBeard.UI.csproj" />
```

## Architecture

```
IUI<TSelf>                      (top-level, e.g. FactionUI)
 +-- IUIScreen                  (a page/tab, e.g. MembersScreen)
 +-- IUIDialog                  (a modal popup, e.g. ConfirmKickDialog)
```

UIManager registers each `IUI` by type, instantiates every screen and dialog it declares through `Configure(UIBuilder)`, and caches the instances. All navigation is generic — `OpenUI<TUI>()`, `SetScreen<TScreen>()`, `OpenDialog<TDialog>()`.

## Setup

```csharp
using BlueBeard.UI;

var uiManager = new UIManager();
uiManager.Load();

uiManager.RegisterUI<FactionUI>();
// ... later, when the player requests the UI:
uiManager.OpenUI<FactionUI>(player);

// On plugin unload:
uiManager.Unload();
```

## Declaring a UI

Inherit from `UIBase` for virtual no-op defaults and implement `IUI<TSelf>` for registration:

```csharp
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

    public override void OnButtonPressed(UIContext ctx, string buttonName)
    {
        switch (buttonName)
        {
            case "Faction_Close":        MyPlugin.UI.CloseUI(ctx.Player); return;
            case "Faction_Tab_Overview": MyPlugin.UI.SetScreen<FactionOverviewScreen>(ctx.Player); return;
            case "Faction_Tab_Members":  MyPlugin.UI.SetScreen<FactionMembersScreen>(ctx.Player); return;
        }

        if (ctx.Component.CurrentDialog != null)
            ctx.Component.CurrentDialog.OnButtonPressed(ctx, buttonName);
        else
            ctx.Component.CurrentScreen?.OnButtonPressed(ctx, buttonName);
    }
}
```

Screens and dialogs inherit `UIScreenBase` / `UIDialogBase`, each with a public parameterless constructor.

## UIManager API

| Method | Description |
|--------|-------------|
| `RegisterUI<TUI>()` | Instantiate, configure, and cache a UI plus every screen/dialog it declares |
| `OpenUI<TUI>(player)` | Send the effect, enable modal, show the default screen |
| `CloseUI(player)` | Run full close lifecycle, clear effect, disable modal, reset component |
| `SetScreen<TScreen>(player)` | Transition to a different screen on the active UI |
| `OpenDialog<TDialog>(player)` | Open a dialog registered on the active UI |
| `CloseDialog(player)` | Close the active dialog |
| `GetUI<TUI>() / GetScreen<T>() / GetDialog<T>()` | Retrieve cached instance for state inspection |
| `PushUpdate(player, key, value)` | Dispatch an update through dialog → screen → UI |
| `PushUpdateAll<TUI>(key, value)` | Broadcast to every player with `TUI` open |
| `PushUpdateToScreen<TScreen>(key, value)` | Broadcast to every player on a specific screen |

UIManager automatically hooks `EffectManager.onEffectButtonClicked` and `onEffectTextCommitted`, resets per-player state on disconnect, and closes all open UIs on unload.

## Push Updates

External managers notify the active UI of state changes without holding references:

```csharp
// In RentManager after collecting rent (main thread):
MyPlugin.UI.PushUpdate(ownerPlayer, "rent.collected", new Dictionary<string, object>
{
    ["renter"] = renterName,
    ["amount"] = property.RentPrice,
});
```

The update travels dialog → screen → IUI; the first `OnUpdate` that returns `true` consumes it. Default (inherited from the abstract base classes) is `false` (not handled), so updates naturally propagate.

```csharp
public class ManagementScreen : UIScreenBase
{
    public override string Id => "management";

    public override bool OnUpdate(UIContext ctx, string key, object value)
    {
        if (key != "rent.collected") return false;
        var data = (Dictionary<string, object>)value;
        EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
            "Canvas/RentStatus", $"Last payment: {data["amount"]} EXP");
        return true;
    }
}
```

Thread safety: `PushUpdate` calls `EffectManager` and must run on the main thread. Wrap in `ThreadHelper.RunSynchronously` if dispatching from a background worker.

## UIContext

Every callback receives a `UIContext`:

| Property | Type | Description |
|----------|------|-------------|
| `Player` | `UnturnedPlayer` | The Rocket player |
| `Connection` | `ITransportConnection` | For `EffectManager` calls |
| `EffectKey` | `short` | The active UI's effect key |
| `Component` | `UIPlayerComponent` | Per-player state (`CurrentUI`, `CurrentScreen`, `CurrentDialog`, `IsOpen`, `State`) |

## Per-Player State

`UIPlayerComponent` persists across callbacks within a single UI session. Use its `State` dictionary for pagination, selection, pending input, etc., with dot-namespaced keys:

```csharp
ctx.Component.State["members.page"] = 0;
ctx.Component.State["members.selectedId"] = steamId;
```

`State` is cleared by `CloseUI` and on player disconnect, not by `SetScreen`.

## Documentation

Full reference and examples in the [Infrastructure docs](../docs/UI/Home.md).

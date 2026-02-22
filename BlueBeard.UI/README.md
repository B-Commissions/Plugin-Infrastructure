# BlueBeard.UI

A reusable framework for building full-screen UIs using Unturned's `EffectManager` API. Provides a hierarchical IUI/IUIScreen/IUIDialog model with automatic event routing, per-player state, modal management, and cleanup.

## Installation

Add a project reference:

```xml
<ProjectReference Include="..\BlueBeard.UI\BlueBeard.UI.csproj" />
```

## Architecture

```
IUI (top-level container, e.g. FactionUI)
  -> IUIScreen (a page/tab, e.g. MembersScreen)
       -> IUIDialog (a popup within a screen, e.g. ConfirmKickDialog)
```

Each layer handles its own button/input logic independently.

## Event Routing

```
EffectManager.onEffectButtonClicked / onEffectTextCommitted
  -> UIManager (looks up player's UIPlayerComponent)
    -> IUI.OnButtonPressed / OnTextSubmitted
      -> You route to the active IUIScreen
        -> Screen routes to the active IUIDialog (if one is open)
```

UIManager delivers events to `IUI`. The `IUI` implementation is responsible for routing to screens/dialogs.

## Setup

```csharp
using BlueBeard.UI;

// In your plugin's Load() or OnLevelLoaded():
var uiManager = new UIManager();
uiManager.Load();

// On unload:
uiManager.Unload(); // closes all open UIs, unhooks events
```

## UIManager API

| Method | Description |
|--------|-------------|
| `RegisterUI(IUI ui)` | Register a UI so it can be opened |
| `OpenUI(player, ui)` | Send the effect, enable modal, call `OnOpened` + `DefaultScreen.OnShow` |
| `CloseUI(player)` | Call `OnHide`/`OnClosed`, clear the effect, disable modal, reset component |
| `SetScreen(player, screen)` | Close any open dialog, call `OnHide` on old screen, `OnShow` on new |
| `OpenDialog(player, dialog)` | Close any existing dialog, call `OnShow` on the new one |
| `CloseDialog(player)` | Call `OnHide` on the active dialog |

UIManager automatically:
- Hooks `EffectManager.onEffectButtonClicked` and `onEffectTextCommitted`
- Cleans up on player disconnect (`Provider.onEnemyDisconnected`)
- Closes all open UIs on plugin unload

## UIContext

Every callback receives a `UIContext` with:

| Property | Type | Description |
|----------|------|-------------|
| `Player` | `UnturnedPlayer` | The Rocket player |
| `Connection` | `ITransportConnection` | For `EffectManager` calls |
| `EffectKey` | `short` | The active UI's effect key |
| `Component` | `UIPlayerComponent` | Per-player state (current screen, dialog, State dict) |

## Per-Player State

`UIPlayerComponent` is attached to the player's GameObject. It tracks:

```csharp
comp.CurrentUI       // which IUI is open
comp.CurrentScreen   // which IUIScreen is active
comp.CurrentDialog   // which IUIDialog is active (null if none)
comp.IsOpen          // whether any UI is open
comp.State           // Dictionary<string, object> for arbitrary data
```

Use `State` for things like pagination, selected IDs, or pending input. Namespace keys by convention:

```csharp
ctx.Component.State["members.page"] = 0;
ctx.Component.State["members.selectedId"] = steamId;
var page = (int)ctx.Component.State["members.page"];
```

State is cleared automatically when the UI is closed or the player disconnects.

---

## How to Build a UI

### 1. Create the IUI implementation

```csharp
using BlueBeard.UI;

public class FactionUI : IUI
{
    public string Id => "faction";
    public ushort EffectId => 50600;
    public short EffectKey => (short)EffectId;
    public IUIScreen[] Screens => [OverviewScreen, MembersScreen];
    public IUIScreen DefaultScreen => OverviewScreen;

    public FactionOverviewScreen OverviewScreen { get; } = new();
    public FactionMembersScreen MembersScreen { get; } = new();

    public void OnOpened(UIContext ctx)
    {
        // populate initial data, set visibility, etc.
    }

    public void OnClosed(UIContext ctx)
    {
        // cleanup if needed
    }

    public void OnButtonPressed(UIContext ctx, string buttonName)
    {
        // 1. Handle UI-global buttons (close, tab switches)
        if (buttonName == "Faction_Close")
        {
            uiManager.CloseUI(ctx.Player);
            return;
        }
        if (buttonName == "Faction_Tab_Members")
        {
            uiManager.SetScreen(ctx.Player, MembersScreen);
            return;
        }

        // 2. Route to active dialog first, then screen
        if (ctx.Component.CurrentDialog != null)
            ctx.Component.CurrentDialog.OnButtonPressed(ctx, buttonName);
        else
            ctx.Component.CurrentScreen?.OnButtonPressed(ctx, buttonName);
    }

    public void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        if (ctx.Component.CurrentDialog != null)
            ctx.Component.CurrentDialog.OnTextSubmitted(ctx, inputName, text);
        else
            ctx.Component.CurrentScreen?.OnTextSubmitted(ctx, inputName, text);
    }
}
```

### 2. Create Screens

```csharp
using BlueBeard.UI;
using SDG.Unturned;

public class FactionMembersScreen : IUIScreen
{
    public string Id => "members";
    public IUIDialog[] Dialogs => [ConfirmKickDialog];

    public ConfirmKickDialog ConfirmKickDialog { get; } = new();

    public void OnShow(UIContext ctx)
    {
        ctx.Component.State["members.page"] = 0;
        RefreshList(ctx);

        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel", true);
    }

    public void OnHide(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/MembersPanel", false);
    }

    public void OnButtonPressed(UIContext ctx, string buttonName)
    {
        switch (buttonName)
        {
            case "Members_NextPage":
                ctx.Component.State["members.page"] = (int)ctx.Component.State["members.page"] + 1;
                RefreshList(ctx);
                break;

            case "Members_Kick":
                uiManager.OpenDialog(ctx.Player, ConfirmKickDialog);
                break;
        }
    }

    public void OnTextSubmitted(UIContext ctx, string inputName, string text)
    {
        if (inputName == "Members_Search")
        {
            ctx.Component.State["members.search"] = text;
            ctx.Component.State["members.page"] = 0;
            RefreshList(ctx);
        }
    }

    private void RefreshList(UIContext ctx)
    {
        // fetch data, update UI text/visibility via EffectManager calls
    }
}
```

### 3. Create Dialogs

```csharp
using BlueBeard.UI;
using SDG.Unturned;

public class ConfirmKickDialog : IUIDialog
{
    public string Id => "confirm_kick";

    public void OnShow(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/ConfirmKickPanel", true);
    }

    public void OnHide(UIContext ctx)
    {
        EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
            "Canvas/ConfirmKickPanel", false);
    }

    public void OnButtonPressed(UIContext ctx, string buttonName)
    {
        switch (buttonName)
        {
            case "ConfirmKick_Yes":
                // perform the kick
                uiManager.CloseDialog(ctx.Player);
                break;

            case "ConfirmKick_No":
                uiManager.CloseDialog(ctx.Player);
                break;
        }
    }

    public void OnTextSubmitted(UIContext ctx, string inputName, string text) { }
}
```

### 4. Register and Open

```csharp
// During plugin init:
var factionUI = new FactionUI();
uiManager.RegisterUI(factionUI);

// When a player runs a command or triggers the UI:
uiManager.OpenUI(unturnedPlayer, factionUI);
```

---

## Common EffectManager Calls

These are the Unturned API calls you'll use inside `OnShow`/`OnHide`/button handlers:

```csharp
// Show/hide a Unity GameObject by name
EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
    "Canvas/Panel/ElementName", visible);

// Set text on a Unity Text/TMP element
EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
    "Canvas/Panel/Label", "Hello World");

// Set an image URL
EffectManager.sendUIEffectImageURL(ctx.EffectKey, ctx.Connection, true,
    "Canvas/Panel/Image", "https://example.com/image.png");
```

All of these use `ctx.EffectKey` and `ctx.Connection` from the `UIContext`.

## Async Data Loading

Use `ThreadHelper` to load data off the main thread, then update the UI on the main thread:

```csharp
public void OnShow(UIContext ctx)
{
    EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
        "Canvas/Panel/Loading", true);

    ThreadHelper.RunAsynchronously(async () =>
    {
        var members = await db.Table<Member>().Where(m => m.FactionId == factionId);

        ThreadHelper.RunSynchronously(() =>
        {
            EffectManager.sendUIEffectVisibility(ctx.EffectKey, ctx.Connection, true,
                "Canvas/Panel/Loading", false);

            for (int i = 0; i < members.Count; i++)
                EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
                    $"Canvas/Panel/Member_{i}/Name", members[i].Name);
        });
    });
}
```

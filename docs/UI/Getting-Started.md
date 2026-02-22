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
      +-- IUIDialog  (a modal popup within a screen, e.g. ConfirmKickDialog)
```

- **IUI** owns one or more screens and a `DefaultScreen`. It receives all button and text events from UIManager and is responsible for routing them to the active screen or dialog.
- **IUIScreen** represents a full page of content. Only one screen is active at a time. Each screen can own zero or more dialogs.
- **IUIDialog** is a popup overlay shown on top of a screen. Only one dialog can be open at a time within a screen.

---

## UIManager API

`UIManager` is the central coordinator. It implements `IManager`, so you call `Load()` during plugin startup and `Unload()` during plugin shutdown.

| Method | Description |
|--------|-------------|
| `RegisterUI(IUI ui)` | Register a UI so the manager knows about it. Call this once per UI during plugin init. |
| `OpenUI(UnturnedPlayer player, IUI ui)` | Send the UI effect to the player, enable the modal cursor, set `DefaultScreen` as active, and call `OnOpened` + `DefaultScreen.OnShow`. If the player already has a UI open, it is closed first. |
| `CloseUI(UnturnedPlayer player)` | Hide the active dialog (if any), hide the current screen, call `OnClosed`, clear the effect, disable the modal cursor, and reset the player component. |
| `SetScreen(UnturnedPlayer player, IUIScreen screen)` | Switch screens. Closes any open dialog, calls `OnHide` on the old screen, sets the new screen, and calls `OnShow`. |
| `OpenDialog(UnturnedPlayer player, IUIDialog dialog)` | Open a dialog. If another dialog is already open it is closed first. Calls `OnShow` on the new dialog. |
| `CloseDialog(UnturnedPlayer player)` | Close the active dialog and call its `OnHide`. |

### Initialization

```csharp
// In your plugin's Load() or OnLevelLoaded:
var uiManager = new UIManager();
uiManager.Load();

// Register your UIs:
var factionUI = new FactionUI();
uiManager.RegisterUI(factionUI);

// In your plugin's Unload():
uiManager.Unload();  // closes all open UIs, unhooks events, clears registrations
```

---

## Event Flow

When a player clicks a button or submits text in an Unturned UI effect, the event travels through the following path:

```
EffectManager.onEffectButtonClicked / onEffectTextCommitted
  |
  v
UIManager  (looks up the player's UIPlayerComponent)
  |  -- if no UI is open, event is silently ignored
  v
IUI.OnButtonPressed(context, buttonName)
  or
IUI.OnTextSubmitted(context, inputName, text)
  |
  v
Your IUI implementation routes to:
  1. Global buttons (close, tab switches)
  2. CurrentDialog.OnButtonPressed (if a dialog is open)
  3. CurrentScreen.OnButtonPressed (otherwise)
```

UIManager only calls the IUI layer. The IUI implementation is responsible for routing events further to the correct screen and dialog.

---

## Lifecycle

### Opening a UI

```
OpenUI(player, ui)
  1. If the player already has a UI open, CloseUI is called first
  2. EffectManager.sendUIEffect(effectId, effectKey, connection, true)
  3. Player modal cursor is enabled (EPluginWidgetFlags.Modal)
  4. UIPlayerComponent is initialized: CurrentUI = ui, CurrentScreen = DefaultScreen, IsOpen = true
  5. ui.OnOpened(context)
  6. ui.DefaultScreen.OnShow(context)
```

### Switching Screens

```
SetScreen(player, newScreen)
  1. If a dialog is open, dialog.OnHide(context) is called and CurrentDialog is set to null
  2. CurrentScreen.OnHide(context)
  3. CurrentScreen is set to newScreen
  4. newScreen.OnShow(context)
```

### Opening a Dialog

```
OpenDialog(player, dialog)
  1. If another dialog is already open, its OnHide(context) is called
  2. CurrentDialog is set to dialog
  3. dialog.OnShow(context)
```

### Closing a Dialog

```
CloseDialog(player)
  1. CurrentDialog.OnHide(context)
  2. CurrentDialog is set to null
```

### Closing a UI

```
CloseUI(player)
  1. If a dialog is open, dialog.OnHide(context) is called and CurrentDialog is set to null
  2. CurrentScreen.OnHide(context)
  3. ui.OnClosed(context)
  4. EffectManager.askEffectClearByID(effectId, connection)
  5. Player modal cursor is disabled
  6. UIPlayerComponent.Reset() -- clears CurrentUI, CurrentScreen, CurrentDialog, IsOpen, and State
```

### Automatic Cleanup

UIManager hooks two events for automatic cleanup:

- **Player disconnect** (`Provider.onEnemyDisconnected`): the player's `UIPlayerComponent` is reset. No `OnHide`/`OnClosed` callbacks are fired because the player is already gone.
- **Plugin unload** (`Unload()`): iterates every online player, and if they have an open UI, calls `CloseUI` on them (which fires the full close lifecycle). Then clears all registered UIs and unhooks events.

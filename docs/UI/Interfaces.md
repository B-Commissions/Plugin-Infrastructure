# Interface Reference

All interfaces live in the `BlueBeard.UI` namespace.

---

## IUI

The top-level container for a full-screen UI. One `IUI` instance represents one distinct menu (e.g. a faction panel, a shop, an admin tool).

```csharp
namespace BlueBeard.UI;

public interface IUI
{
    string Id { get; }
    ushort EffectId { get; }
    short EffectKey { get; }
    IUIScreen[] Screens { get; }
    IUIScreen DefaultScreen { get; }

    void OnOpened(UIContext context);
    void OnClosed(UIContext context);
    void OnButtonPressed(UIContext context, string buttonName);
    void OnTextSubmitted(UIContext context, string inputName, string text);
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | A unique identifier for this UI (e.g. `"faction"`, `"shop"`). Used as the key in UIManager's internal registry. |
| `EffectId` | `ushort` | The asset ID of the Unity effect bundle. Passed to `EffectManager.sendUIEffect` when the UI is opened. |
| `EffectKey` | `short` | The key used in subsequent `EffectManager` calls (`sendUIEffectVisibility`, `sendUIEffectText`, etc.). Typically set to `(short)EffectId`. |
| `Screens` | `IUIScreen[]` | All screens belonging to this UI. Used for reference; UIManager does not iterate this array. |
| `DefaultScreen` | `IUIScreen` | The screen that is shown automatically when the UI is opened via `OpenUI`. Must be one of the entries in `Screens`. |

### Methods

| Method | When Called |
|--------|------------|
| `OnOpened(UIContext context)` | Immediately after the effect is sent to the player and the component is initialized, but before `DefaultScreen.OnShow`. Use this to populate initial data that is shared across all screens. |
| `OnClosed(UIContext context)` | After the current screen's `OnHide` has been called and before the effect is cleared. Use this for any cleanup that spans the entire UI. |
| `OnButtonPressed(UIContext context, string buttonName)` | Every time the player clicks a button in this UI's effect. You are responsible for routing: handle global buttons (close, tab switches) first, then delegate to `CurrentDialog` or `CurrentScreen`. |
| `OnTextSubmitted(UIContext context, string inputName, string text)` | Every time the player submits text in an input field. Same routing responsibility as `OnButtonPressed`. |

---

## IUIScreen

A page or tab within an `IUI`. Only one screen is active at a time per player.

```csharp
namespace BlueBeard.UI;

public interface IUIScreen
{
    string Id { get; }
    IUIDialog[] Dialogs { get; }

    void OnShow(UIContext context);
    void OnHide(UIContext context);
    void OnButtonPressed(UIContext context, string buttonName);
    void OnTextSubmitted(UIContext context, string inputName, string text);
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | A unique identifier within the parent UI (e.g. `"overview"`, `"members"`). |
| `Dialogs` | `IUIDialog[]` | All dialogs that can be opened from this screen. Used for reference; UIManager does not iterate this array. |

### Methods

| Method | When Called |
|--------|------------|
| `OnShow(UIContext context)` | When this screen becomes the active screen -- either because the UI was just opened (and this is the default screen) or because `SetScreen` was called. Use this to initialize per-screen state and show the screen's UI elements via `sendUIEffectVisibility`. |
| `OnHide(UIContext context)` | When this screen is no longer active -- either because `SetScreen` switched to another screen, or because `CloseUI` was called. Use this to hide the screen's UI elements. |
| `OnButtonPressed(UIContext context, string buttonName)` | Called by the parent `IUI` when this screen is active and no dialog is intercepting the event. Handle screen-specific buttons here (pagination, row selection, etc.). |
| `OnTextSubmitted(UIContext context, string inputName, string text)` | Called by the parent `IUI` when this screen is active and no dialog is intercepting the event. Handle screen-specific text inputs here (search fields, name inputs, etc.). |

---

## IUIDialog

A modal popup shown on top of a screen. Only one dialog can be open at a time.

```csharp
namespace BlueBeard.UI;

public interface IUIDialog
{
    string Id { get; }

    void OnShow(UIContext context);
    void OnHide(UIContext context);
    void OnButtonPressed(UIContext context, string buttonName);
    void OnTextSubmitted(UIContext context, string inputName, string text);
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | A unique identifier within the parent screen (e.g. `"confirm_kick"`, `"select_rank"`). |

### Methods

| Method | When Called |
|--------|------------|
| `OnShow(UIContext context)` | When `OpenDialog` is called for this dialog. Use this to show the dialog's overlay panel. |
| `OnHide(UIContext context)` | When `CloseDialog` is called, or when a different dialog replaces this one, or when the screen is switched, or when the UI is closed. Use this to hide the dialog's overlay panel. |
| `OnButtonPressed(UIContext context, string buttonName)` | Called by the parent `IUI` (which routes through the screen) when this dialog is the active dialog. Handle confirm/cancel buttons, option selections, etc. |
| `OnTextSubmitted(UIContext context, string inputName, string text)` | Called by the parent `IUI` when this dialog is active. Handle any text inputs within the dialog. |

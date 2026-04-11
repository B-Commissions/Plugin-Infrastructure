# Interface Reference

All interfaces and base classes live in the `BlueBeard.UI` namespace.

---

## IUI

The top-level container for a full-screen UI. One `IUI` instance represents one distinct menu (e.g. a faction panel, a shop, an admin tool). Concrete implementations must also implement `IUI<TSelf>` (see below) to be registrable via `UIManager.RegisterUI<TUI>()`.

```csharp
namespace BlueBeard.UI;

public interface IUI
{
    string Id { get; }
    ushort EffectId { get; }
    short EffectKey { get; }

    void OnOpened(UIContext context);
    void OnClosed(UIContext context);
    void OnButtonPressed(UIContext context, string buttonName);
    void OnTextSubmitted(UIContext context, string inputName, string text);
    bool OnUpdate(UIContext context, string key, object value);
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | A unique identifier for this UI (e.g. `"faction"`, `"shop"`). Informational; the registry is type-keyed, not id-keyed. |
| `EffectId` | `ushort` | The asset ID of the Unity effect bundle. Passed to `EffectManager.sendUIEffect` when the UI is opened. |
| `EffectKey` | `short` | The key used in subsequent `EffectManager` calls (`sendUIEffectVisibility`, `sendUIEffectText`, etc.). Typically set to `(short)EffectId`. |

### Methods

| Method | When Called |
|--------|------------|
| `OnOpened(UIContext context)` | Immediately after the effect is sent to the player and the component is initialised, but before the default screen's `OnShow`. Use this to populate initial data shared across all screens. |
| `OnClosed(UIContext context)` | After the current screen's `OnHide` has been called and before the effect is cleared. Use for any cleanup that spans the entire UI. |
| `OnButtonPressed(UIContext context, string buttonName)` | Every time the player clicks a button in this UI's effect. You are responsible for routing: handle global buttons (close, tab switches) first, then delegate to `CurrentDialog` or `CurrentScreen`. |
| `OnTextSubmitted(UIContext context, string inputName, string text)` | Every time the player submits text in an input field. Same routing responsibility as `OnButtonPressed`. |
| `OnUpdate(UIContext context, string key, object value)` | Called last in the push-update dispatch chain (after any active dialog and the active screen). Return `true` if this UI handled the update, `false` to indicate it was unhandled. |

---

## IUI&lt;TSelf&gt;

Self-referential generic marker that unlocks registration via `UIManager.RegisterUI<TUI>()`. Concrete UIs implement `IUI<TSelf>` where `TSelf` is the implementing type itself (CRTP).

```csharp
public interface IUI<TSelf> : IUI where TSelf : IUI<TSelf>
{
    void Configure(UIBuilder builder);
}
```

### Methods

| Method | When Called |
|--------|------------|
| `Configure(UIBuilder builder)` | Called exactly once, during `RegisterUI<TUI>()`. Use the builder to declare every screen and dialog that belongs to this UI. UIManager instantiates each declared type and caches the result. |

---

## UIBuilder

Fluent configurator passed to `Configure`. Accumulates the set of screen and dialog types that belong to a UI.

```csharp
public class UIBuilder
{
    public UIBuilder AddScreen<TScreen>() where TScreen : IUIScreen, new();
    public UIBuilder AddScreen<TScreen>(bool isDefault) where TScreen : IUIScreen, new();
    public UIBuilder AddDialog<TDialog>() where TDialog : IUIDialog, new();
}
```

### Semantics

- **Default screen**: the first screen registered is the default unless you override with `AddScreen<T>(isDefault: false)` or pass `isDefault: true` to a later call. The default screen is the one shown automatically by `OpenUI`.
- **Parameterless constructors**: every screen and dialog type must have a public parameterless constructor (the `new()` constraint). Access plugin dependencies through a static singleton rather than constructor injection.
- **One instance per type**: if you register the same screen or dialog type twice, only one instance is cached; subsequent `AddScreen<T>` / `AddDialog<T>` calls are no-ops.

### Example

```csharp
public void Configure(UIBuilder builder)
{
    builder
        .AddScreen<FactionOverviewScreen>(isDefault: true)
        .AddScreen<FactionMembersScreen>()
        .AddScreen<FactionSettingsScreen>()
        .AddDialog<ConfirmKickDialog>()
        .AddDialog<ConfirmDisbandDialog>();
}
```

---

## IUIScreen

A page or tab within an `IUI`. Only one screen is active at a time per player.

```csharp
namespace BlueBeard.UI;

public interface IUIScreen
{
    string Id { get; }

    void OnShow(UIContext context);
    void OnHide(UIContext context);
    void OnButtonPressed(UIContext context, string buttonName);
    void OnTextSubmitted(UIContext context, string inputName, string text);
    bool OnUpdate(UIContext context, string key, object value);
}
```

Note: `IUIScreen` no longer has a `Dialogs` property. Dialogs are registered at the UI level via `UIBuilder.AddDialog<T>()` and are available to every screen in the UI.

### Methods

| Method | When Called |
|--------|------------|
| `OnShow(UIContext context)` | When this screen becomes the active screen -- either because the UI was just opened and this is the default screen, or because `SetScreen<T>` was called. Use this to initialise per-screen state and show the screen's UI elements via `sendUIEffectVisibility`. |
| `OnHide(UIContext context)` | When this screen is no longer active -- either because `SetScreen<T>` switched to another screen, or because `CloseUI` was called. Use this to hide the screen's UI elements. |
| `OnButtonPressed(UIContext context, string buttonName)` | Called by the parent `IUI` when this screen is active and no dialog is intercepting the event. Handle screen-specific buttons here (pagination, row selection, etc.). |
| `OnTextSubmitted(UIContext context, string inputName, string text)` | Called by the parent `IUI` when this screen is active and no dialog is intercepting the event. |
| `OnUpdate(UIContext context, string key, object value)` | Called after any active dialog and before the parent `IUI` in the push-update dispatch chain. Return `true` if handled. |

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
    bool OnUpdate(UIContext context, string key, object value);
}
```

### Methods

| Method | When Called |
|--------|------------|
| `OnShow(UIContext context)` | When `OpenDialog<T>` is called for this dialog. Use this to show the dialog's overlay panel. |
| `OnHide(UIContext context)` | When `CloseDialog` is called, when a different dialog replaces this one, when the screen is switched, or when the UI is closed. |
| `OnButtonPressed(UIContext context, string buttonName)` | Called by the parent `IUI` when this dialog is the active dialog. |
| `OnTextSubmitted(UIContext context, string inputName, string text)` | Called by the parent `IUI` when this dialog is active. |
| `OnUpdate(UIContext context, string key, object value)` | Called first in the push-update dispatch chain. Return `true` to consume the update and stop propagation; return `false` to let it continue to the screen and then the UI. |

---

## Abstract base classes

`BlueBeard.UI` also exposes three abstract base classes so subclasses only override what they need. Each provides virtual no-op implementations of every callback. Use these when the curiously-recurring template of `IUI<TSelf>` + the virtual no-ops gives you everything you want out of the box.

```csharp
public abstract class UIBase : IUI
{
    public abstract string Id { get; }
    public abstract ushort EffectId { get; }
    public abstract short EffectKey { get; }

    public virtual void OnOpened(UIContext context) { }
    public virtual void OnClosed(UIContext context) { }
    public virtual void OnButtonPressed(UIContext context, string buttonName) { }
    public virtual void OnTextSubmitted(UIContext context, string inputName, string text) { }
    public virtual bool OnUpdate(UIContext context, string key, object value) => false;
}

public abstract class UIScreenBase : IUIScreen { /* same pattern */ }
public abstract class UIDialogBase : IUIDialog { /* same pattern */ }
```

`UIBase` implements `IUI` but **not** `IUI<TSelf>` (because `TSelf` is inherently unknown to the base). Concrete UIs combine both:

```csharp
public class FactionUI : UIBase, IUI<FactionUI>
{
    public override string Id => "faction";
    public override ushort EffectId => 50600;
    public override short EffectKey => (short)EffectId;

    public void Configure(UIBuilder builder) { /* ... */ }

    // Override only the callbacks you care about:
    public override void OnButtonPressed(UIContext ctx, string buttonName) { /* ... */ }
}
```

Using the interfaces directly without the base classes is still supported; you just have to implement every member yourself (including `OnUpdate => false`). The base classes exist purely to remove that boilerplate.

---

## The net481 default-interface-method caveat

C# 8 default interface implementations are not supported at runtime on .NET Framework 4.8.1. That's why `OnUpdate` is a **required** interface member rather than a default -- every interface implementer must provide a body. The `UIBase` / `UIScreenBase` / `UIDialogBase` classes exist to give you virtual no-op defaults via inheritance instead of interface defaults.

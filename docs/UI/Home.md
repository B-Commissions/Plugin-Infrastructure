# BlueBeard.UI

BlueBeard.UI is a framework for building full-screen UIs using Unturned's `EffectManager` API. It sits on top of BlueBeard.Core and provides everything you need to ship multi-screen, interactive menus without writing boilerplate event-wiring code.

## Features

- **Generic-type-resolved registration** -- `RegisterUI<TUI>()` / `OpenUI<TUI>()` / `SetScreen<TScreen>()` / `OpenDialog<TDialog>()` -- no instance references to hold and pass around.
- **Hierarchical IUI -> IUIScreen -> IUIDialog model** -- organise UI into a top-level container, switchable pages/tabs, and modal popup dialogs.
- **UIBuilder declaration** -- each `IUI<TSelf>` declares its screens and dialogs in a `Configure(UIBuilder)` method. UIManager instantiates and caches them once during registration.
- **Abstract base classes** -- `UIBase`, `UIScreenBase`, `UIDialogBase` provide virtual no-op implementations so subclasses only override what they need.
- **Push update system** -- `PushUpdate`, `PushUpdateAll<TUI>`, `PushUpdateToScreen<TScreen>` let external managers notify the active UI of state changes. Dispatch chain: dialog → screen → IUI, first handler that returns `true` consumes the update.
- **Automatic event routing for buttons and text inputs** -- UIManager hooks `EffectManager.onEffectButtonClicked` and `onEffectTextCommitted` globally and delivers events to the correct IUI instance.
- **Per-player state management (UIPlayerComponent)** -- a `MonoBehaviour` attached to each player tracks which UI, screen, and dialog are active, plus an arbitrary `Dictionary<string, object>` for page numbers, selected IDs, pending input, and anything else.
- **Modal cursor management** -- `OpenUI` enables the modal cursor flag automatically; `CloseUI` disables it.
- **Automatic cleanup on disconnect and unload** -- player disconnect resets the component; plugin unload closes every open UI for every online player and unhooks all events.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Project setup, architecture, registration via `UIBuilder`, push updates overview |
| [Interfaces](Interfaces.md) | Full reference for IUI, IUI&lt;TSelf&gt;, IUIScreen, IUIDialog, UIBuilder |
| [Player State](Player-State.md) | UIContext and UIPlayerComponent -- per-player tracking and custom state |
| [Event Routing](Event-Routing.md) | Button / text input routing and push-update dispatch |
| [Examples](Examples.md) | Multi-screen faction UI with push updates |

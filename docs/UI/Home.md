# BlueBeard.UI

BlueBeard.UI is a framework for building full-screen UIs using Unturned's `EffectManager` API. It sits on top of BlueBeard.Core and provides everything you need to ship multi-screen, interactive menus without writing boilerplate event-wiring code.

## Features

- **Hierarchical IUI -> IUIScreen -> IUIDialog model** -- organize your UI into a top-level container, switchable pages/tabs, and modal popup dialogs.
- **Automatic event routing for buttons and text inputs** -- UIManager hooks `EffectManager.onEffectButtonClicked` and `onEffectTextCommitted` globally and delivers events to the correct IUI instance.
- **Per-player state management (UIPlayerComponent)** -- a `MonoBehaviour` attached to each player tracks which UI, screen, and dialog are active, plus an arbitrary `Dictionary<string, object>` for page numbers, selected IDs, pending input, and anything else.
- **Modal cursor management** -- `OpenUI` enables the modal cursor flag automatically; `CloseUI` disables it.
- **Automatic cleanup on disconnect and unload** -- player disconnect resets the component; plugin unload closes every open UI for every online player and unhooks all events.

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](Getting-Started.md) | Project setup, architecture overview, lifecycle, and event flow |
| [Interfaces](Interfaces.md) | Full reference for IUI, IUIScreen, and IUIDialog |
| [Player State](Player-State.md) | UIContext and UIPlayerComponent -- per-player tracking and custom state |
| [Event Routing](Event-Routing.md) | How button clicks and text inputs travel from EffectManager to your code |
| [Examples](Examples.md) | Complete implementation examples: multi-screen UI, pagination, dialogs, async loading |

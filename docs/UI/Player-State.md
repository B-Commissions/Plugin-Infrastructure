# Player State

BlueBeard.UI tracks per-player UI state through two classes: `UIContext` (an immutable snapshot passed to every callback) and `UIPlayerComponent` (a persistent `MonoBehaviour` attached to the player's `GameObject`).

---

## UIContext

Every callback in `IUI`, `IUIScreen`, and `IUIDialog` receives a `UIContext`. It is a convenience object that bundles everything you need to interact with the player's UI.

```csharp
namespace BlueBeard.UI;

public class UIContext
{
    public UnturnedPlayer Player { get; }
    public ITransportConnection Connection { get; }
    public short EffectKey { get; }
    public UIPlayerComponent Component { get; }
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Player` | `UnturnedPlayer` | The Rocket player this context belongs to. Use this for non-UI operations (sending chat messages, getting Steam ID, etc.). |
| `Connection` | `ITransportConnection` | The player's transport connection. Required as the second argument to all `EffectManager.sendUIEffect*` calls. |
| `EffectKey` | `short` | The active UI's effect key. Required as the first argument to all `EffectManager.sendUIEffect*` calls. |
| `Component` | `UIPlayerComponent` | The persistent per-player component. Gives you access to `CurrentUI`, `CurrentScreen`, `CurrentDialog`, `IsOpen`, and the `State` dictionary. |

### Typical Usage

```csharp
public void OnShow(UIContext ctx)
{
    // Update a text element
    EffectManager.sendUIEffectText(ctx.EffectKey, ctx.Connection, true,
        "Canvas/Panel/Title", "Members");

    // Read from State
    int page = ctx.Component.State.ContainsKey("members.page")
        ? (int)ctx.Component.State["members.page"]
        : 0;

    // Access the player's Steam ID
    ulong steamId = ctx.Player.CSteamID.m_SteamID;
}
```

---

## UIPlayerComponent

`UIPlayerComponent` is a Unity `MonoBehaviour` that UIManager attaches to the player's `GameObject`. It persists for the lifetime of the player's connection and tracks everything about their current UI session.

```csharp
namespace BlueBeard.UI;

public class UIPlayerComponent : MonoBehaviour
{
    public IUI CurrentUI { get; set; }
    public IUIScreen CurrentScreen { get; set; }
    public IUIDialog CurrentDialog { get; set; }
    public bool IsOpen { get; set; }
    public Dictionary<string, object> State { get; }

    public void Reset();
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CurrentUI` | `IUI` | The UI that is currently open for this player, or `null` if no UI is open. |
| `CurrentScreen` | `IUIScreen` | The active screen within the current UI, or `null`. |
| `CurrentDialog` | `IUIDialog` | The active dialog within the current screen, or `null` if no dialog is open. |
| `IsOpen` | `bool` | `true` if a UI is currently open for this player. |
| `State` | `Dictionary<string, object>` | Arbitrary per-player storage. Screens and dialogs use this to persist data across callbacks. |

### Reset

`Reset()` sets `CurrentUI`, `CurrentScreen`, and `CurrentDialog` to `null`, sets `IsOpen` to `false`, and calls `State.Clear()`. It is called automatically by `CloseUI` and on player disconnect.

### How the Component is Created

UIManager uses `GetComponent<UIPlayerComponent>()` to look up the component. If it does not exist yet (first time the player opens any UI), UIManager creates it with `AddComponent<UIPlayerComponent>()`:

```csharp
// Internal to UIManager:
private static UIPlayerComponent GetOrAddComponent(UnturnedPlayer player)
{
    var comp = player.Player.gameObject.GetComponent<UIPlayerComponent>();
    if (comp == null)
        comp = player.Player.gameObject.AddComponent<UIPlayerComponent>();
    return comp;
}
```

You should never need to create or destroy this component yourself. UIManager handles it.

---

## Using the State Dictionary

The `State` dictionary is the primary mechanism for storing per-player data that needs to survive across multiple callbacks within the same UI session. Common use cases:

- **Pagination**: store the current page number
- **Selected items**: store a selected Steam ID, item ID, or row index
- **Pending input**: store text from an input field before a confirm dialog
- **Cached data**: store fetched data to avoid repeated database queries

### Key Naming Convention

Namespace your keys with a dot-separated prefix to avoid collisions between screens and dialogs:

```csharp
// In MembersScreen
ctx.Component.State["members.page"] = 0;
ctx.Component.State["members.selectedId"] = steamId;
ctx.Component.State["members.search"] = searchText;

// In RanksScreen
ctx.Component.State["ranks.page"] = 0;
ctx.Component.State["ranks.selectedRank"] = rankName;

// In ConfirmKickDialog
ctx.Component.State["confirm_kick.targetId"] = targetSteamId;
```

### Reading State Safely

Because `State` stores `object` values, you need to cast when reading. Use `ContainsKey` or `TryGetValue` to avoid `KeyNotFoundException`:

```csharp
// Option 1: ContainsKey
int page = ctx.Component.State.ContainsKey("members.page")
    ? (int)ctx.Component.State["members.page"]
    : 0;

// Option 2: TryGetValue
if (ctx.Component.State.TryGetValue("members.selectedId", out var raw))
{
    ulong selectedId = (ulong)raw;
    // ...
}
```

### Lifetime

State is cleared automatically in two situations:

1. **CloseUI** is called -- `UIPlayerComponent.Reset()` clears the entire `State` dictionary.
2. **Player disconnects** -- `UIPlayerComponent.Reset()` is called via the `Provider.onEnemyDisconnected` hook.

You do not need to clean up State manually. If you switch screens via `SetScreen`, State is **not** cleared -- this lets you share data between screens if needed. If you want to clear screen-specific keys when leaving a screen, do so in `OnHide`.

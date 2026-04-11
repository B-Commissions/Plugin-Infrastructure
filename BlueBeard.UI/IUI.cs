namespace BlueBeard.UI;

public interface IUI
{
    /// <summary>Unique identifier for this UI (e.g., "faction", "shop")</summary>
    string Id { get; }

    /// <summary>The effect asset ID for this UI's Unity asset</summary>
    ushort EffectId { get; }

    /// <summary>The effect key used with EffectManager calls</summary>
    short EffectKey { get; }

    /// <summary>Called when the UI is opened for a player (after effect is sent)</summary>
    void OnOpened(UIContext context);

    /// <summary>Called when the UI is closed for a player (before effect is cleared)</summary>
    void OnClosed(UIContext context);

    /// <summary>Route a button press to the active screen/dialog</summary>
    void OnButtonPressed(UIContext context, string buttonName);

    /// <summary>Route a text input to the active screen/dialog</summary>
    void OnTextSubmitted(UIContext context, string inputName, string text);

    /// <summary>
    /// Receive a push update from an external manager.
    /// Called last in the dispatch chain (after any active dialog and the active screen).
    /// Return true if the update was handled; false to allow the chain to continue.
    /// </summary>
    bool OnUpdate(UIContext context, string key, object value);
}

/// <summary>
/// Self-referential generic marker that unlocks registration via <see cref="UIManager.RegisterUI{TUI}"/>.
/// Implementations declare their screens and dialogs in <see cref="Configure"/>.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself (CRTP).</typeparam>
public interface IUI<TSelf> : IUI where TSelf : IUI<TSelf>
{
    /// <summary>
    /// Called once during <see cref="UIManager.RegisterUI{TUI}"/>. Register screens and dialogs
    /// via the supplied builder.
    /// </summary>
    void Configure(UIBuilder builder);
}

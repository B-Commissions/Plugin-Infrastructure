namespace BlueBeard.UI;

public interface IUI
{
    /// <summary>Unique identifier for this UI (e.g., "faction", "shop")</summary>
    string Id { get; }

    /// <summary>The effect asset ID for this UI's Unity asset</summary>
    ushort EffectId { get; }

    /// <summary>The effect key used with EffectManager calls</summary>
    short EffectKey { get; }

    /// <summary>All screens belonging to this UI</summary>
    IUIScreen[] Screens { get; }

    /// <summary>The default screen shown when the UI opens</summary>
    IUIScreen DefaultScreen { get; }

    /// <summary>Called when the UI is opened for a player (after effect is sent)</summary>
    void OnOpened(UIContext context);

    /// <summary>Called when the UI is closed for a player (before effect is cleared)</summary>
    void OnClosed(UIContext context);

    /// <summary>Route a button press to the active screen/dialog</summary>
    void OnButtonPressed(UIContext context, string buttonName);

    /// <summary>Route a text input to the active screen/dialog</summary>
    void OnTextSubmitted(UIContext context, string inputName, string text);
}

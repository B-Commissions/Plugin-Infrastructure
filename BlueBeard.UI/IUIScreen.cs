namespace BlueBeard.UI;

public interface IUIScreen
{
    /// <summary>Unique identifier within the parent UI (e.g., "overview", "members")</summary>
    string Id { get; }

    /// <summary>Called when this screen becomes the active screen</summary>
    void OnShow(UIContext context);

    /// <summary>Called when this screen is no longer the active screen</summary>
    void OnHide(UIContext context);

    /// <summary>Handle a button press (called by parent UI when this screen is active)</summary>
    void OnButtonPressed(UIContext context, string buttonName);

    /// <summary>Handle a text input (called by parent UI when this screen is active)</summary>
    void OnTextSubmitted(UIContext context, string inputName, string text);

    /// <summary>
    /// Receive a push update from an external manager.
    /// Called after any active dialog and before the parent IUI.
    /// Return true if the update was handled; false to allow the chain to continue.
    /// </summary>
    bool OnUpdate(UIContext context, string key, object value);
}

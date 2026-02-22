namespace BlueBeard.UI;

public interface IUIScreen
{
    /// <summary>Unique identifier within the parent UI (e.g., "overview", "members")</summary>
    string Id { get; }

    /// <summary>All dialogs belonging to this screen</summary>
    IUIDialog[] Dialogs { get; }

    /// <summary>Called when this screen becomes the active screen</summary>
    void OnShow(UIContext context);

    /// <summary>Called when this screen is no longer the active screen</summary>
    void OnHide(UIContext context);

    /// <summary>Handle a button press (called by parent UI when this screen is active)</summary>
    void OnButtonPressed(UIContext context, string buttonName);

    /// <summary>Handle a text input (called by parent UI when this screen is active)</summary>
    void OnTextSubmitted(UIContext context, string inputName, string text);
}

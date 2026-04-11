namespace BlueBeard.UI;

public interface IUIDialog
{
    /// <summary>Unique identifier (e.g., "confirm_kick", "select_rank")</summary>
    string Id { get; }

    /// <summary>Called when this dialog is opened</summary>
    void OnShow(UIContext context);

    /// <summary>Called when this dialog is closed</summary>
    void OnHide(UIContext context);

    /// <summary>Handle a button press (called by UIManager when this dialog is active)</summary>
    void OnButtonPressed(UIContext context, string buttonName);

    /// <summary>Handle a text input (called by UIManager when this dialog is active)</summary>
    void OnTextSubmitted(UIContext context, string inputName, string text);

    /// <summary>
    /// Receive a push update from an external manager.
    /// Called first in the dispatch chain. Return true to consume the update and stop propagation;
    /// return false to allow the chain to continue to the active screen and then the parent IUI.
    /// </summary>
    bool OnUpdate(UIContext context, string key, object value);
}

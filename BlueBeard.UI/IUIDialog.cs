namespace BlueBeard.UI;

public interface IUIDialog
{
    /// <summary>Unique identifier within the parent screen (e.g., "confirm_kick", "select_rank")</summary>
    string Id { get; }

    /// <summary>Called when this dialog is opened</summary>
    void OnShow(UIContext context);

    /// <summary>Called when this dialog is closed</summary>
    void OnHide(UIContext context);

    /// <summary>Handle a button press (called by parent screen when this dialog is active)</summary>
    void OnButtonPressed(UIContext context, string buttonName);

    /// <summary>Handle a text input (called by parent screen when this dialog is active)</summary>
    void OnTextSubmitted(UIContext context, string inputName, string text);
}

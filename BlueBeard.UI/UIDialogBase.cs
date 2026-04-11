namespace BlueBeard.UI;

/// <summary>
/// Abstract convenience base for <see cref="IUIDialog"/> implementations. Provides virtual
/// no-op implementations of every callback so subclasses override only what they need.
/// </summary>
public abstract class UIDialogBase : IUIDialog
{
    public abstract string Id { get; }

    public virtual void OnShow(UIContext context) { }
    public virtual void OnHide(UIContext context) { }
    public virtual void OnButtonPressed(UIContext context, string buttonName) { }
    public virtual void OnTextSubmitted(UIContext context, string inputName, string text) { }
    public virtual bool OnUpdate(UIContext context, string key, object value) => false;
}

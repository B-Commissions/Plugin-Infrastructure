namespace BlueBeard.UI;

/// <summary>
/// Abstract convenience base for <see cref="IUI"/> implementations. Provides virtual no-op
/// implementations of every callback so subclasses override only what they need.
/// Inheritors should also implement <see cref="IUI{TSelf}"/> (e.g. <c>: UIBase, IUI&lt;MyUI&gt;</c>)
/// to expose <see cref="IUI{TSelf}.Configure"/> for registration via
/// <see cref="UIManager.RegisterUI{TUI}"/>.
/// </summary>
public abstract class UIBase : IUI
{
    public abstract string Id { get; }
    public abstract ushort EffectId { get; }
    public abstract short EffectKey { get; }

    public virtual void OnOpened(UIContext context) { }
    public virtual void OnClosed(UIContext context) { }
    public virtual void OnButtonPressed(UIContext context, string buttonName) { }
    public virtual void OnTextSubmitted(UIContext context, string inputName, string text) { }
    public virtual bool OnUpdate(UIContext context, string key, object value) => false;
}

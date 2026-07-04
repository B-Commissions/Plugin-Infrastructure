using SDG.Unturned;

namespace BlueBeard.UI;

/// <summary>
/// Typed helpers over the raw <see cref="EffectManager"/> element calls, keyed off a
/// <see cref="UIContext"/> so screens/dialogs don't repeat the connection/effect-key
/// plumbing on every element write:
///
/// <code>
/// UIElements.SetText(context, "Label_Balance", $"${balance}");
/// UIElements.SetVisible(context, "Panel_Admin", isAdmin);
/// UIElements.SetImage(context, "Icon_Avatar", avatarUrl);
/// </code>
///
/// Main-thread only (all EffectManager sends are). All helpers no-op safely when the
/// context has no live connection (player mid-disconnect).
/// </summary>
public static class UIElements
{
    /// <summary>Set a text element's value.</summary>
    public static void SetText(UIContext context, string elementName, string text)
    {
        if (context?.Connection == null) return;
        EffectManager.sendUIEffectText(context.EffectKey, context.Connection, true, elementName, text);
    }

    /// <summary>Show or hide an element.</summary>
    public static void SetVisible(UIContext context, string elementName, bool visible)
    {
        if (context?.Connection == null) return;
        EffectManager.sendUIEffectVisibility(context.EffectKey, context.Connection, true, elementName, visible);
    }

    /// <summary>Set an image element's URL.</summary>
    public static void SetImage(UIContext context, string elementName, string url, bool shouldCache = true, bool forceRefresh = false)
    {
        if (context?.Connection == null) return;
        EffectManager.sendUIEffectImageURL(context.EffectKey, context.Connection, true, elementName, url, shouldCache, forceRefresh);
    }
}

using System;
using System.Collections.Generic;

namespace BlueBeard.UI;

/// <summary>
/// Passed to <see cref="IUI{TSelf}.Configure"/> during registration.
/// Accumulates the set of screen and dialog types that belong to a UI; UIManager
/// reads these back to instantiate and cache the concrete instances.
/// </summary>
public class UIBuilder
{
    internal Type DefaultScreenType;
    internal readonly List<Type> ScreenTypes = [];
    internal readonly List<Type> DialogTypes = [];

    /// <summary>
    /// Register a screen type. The first screen registered is used as the default.
    /// </summary>
    public UIBuilder AddScreen<TScreen>() where TScreen : IUIScreen, new()
        => AddScreen<TScreen>(isDefault: ScreenTypes.Count == 0);

    /// <summary>
    /// Register a screen type with explicit default marking.
    /// </summary>
    public UIBuilder AddScreen<TScreen>(bool isDefault) where TScreen : IUIScreen, new()
    {
        var type = typeof(TScreen);
        if (!ScreenTypes.Contains(type))
            ScreenTypes.Add(type);
        if (isDefault)
            DefaultScreenType = type;
        return this;
    }

    /// <summary>
    /// Register a dialog type (available to all screens in this UI).
    /// </summary>
    public UIBuilder AddDialog<TDialog>() where TDialog : IUIDialog, new()
    {
        var type = typeof(TDialog);
        if (!DialogTypes.Contains(type))
            DialogTypes.Add(type);
        return this;
    }
}

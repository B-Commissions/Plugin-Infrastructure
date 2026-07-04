using BlueBeard.Core.Abstractions;
using Microsoft.Extensions.Localization;

namespace BlueBeard.OpenMod;

/// <summary>
/// <see cref="ITranslations"/> adapter over OpenMod's <see cref="IStringLocalizer"/>.
/// Missing keys fall back to the key itself. Installed via
/// <see cref="OpenModBootstrap.InstallTranslations"/>.
/// </summary>
public sealed class OpenModTranslations(IStringLocalizer localizer) : ITranslations
{
    public string Translate(string key, params object[] args)
    {
        if (key == null || localizer == null) return key;
        var localized = args is { Length: > 0 } ? localizer[key, args] : localizer[key];
        return localized.ResourceNotFound ? key : localized.Value;
    }
}

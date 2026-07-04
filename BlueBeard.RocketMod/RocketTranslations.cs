using BlueBeard.Core.Abstractions;
using Rocket.API;

namespace BlueBeard.RocketMod;

/// <summary>
/// <see cref="ITranslations"/> adapter over a Rocket plugin's <c>Translations</c> asset.
/// Missing keys fall back to the key itself so callers never receive null.
/// Installed via <see cref="RocketModBootstrap.InstallTranslations"/>.
/// </summary>
public sealed class RocketTranslations(IRocketPlugin plugin) : ITranslations
{
    public string Translate(string key, params object[] args)
    {
        if (key == null) return null;
        var translated = plugin?.Translations?.Instance?.Translate(key, args);
        return string.IsNullOrEmpty(translated) ? key : translated;
    }
}

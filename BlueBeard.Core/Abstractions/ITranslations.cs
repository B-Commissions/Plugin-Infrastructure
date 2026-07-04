namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Framework-agnostic message translation. The RocketMod adapter binds a plugin's
/// Rocket <c>Translations</c>; the OpenMod adapter binds <c>IStringLocalizer</c>.
/// When no adapter installs one, <see cref="BlueBeardHost.Translations"/> falls back to
/// a passthrough that formats the key with the arguments — so libraries can always call
/// <c>Translate</c> without null checks.
/// </summary>
public interface ITranslations
{
    /// <summary>
    /// Translate <paramref name="key"/>, formatting with <paramref name="args"/>.
    /// Implementations should return the key itself when no translation exists.
    /// </summary>
    string Translate(string key, params object[] args);
}

/// <summary>
/// Fallback translator: returns the key (string.Format'd with the arguments when the key
/// contains placeholders and formatting succeeds).
/// </summary>
public sealed class PassthroughTranslations : ITranslations
{
    public string Translate(string key, params object[] args)
    {
        if (key == null) return null;
        if (args == null || args.Length == 0) return key;
        try { return string.Format(key, args); }
        catch (System.FormatException) { return key; }
    }
}

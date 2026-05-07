using System.Collections.Generic;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Framework-agnostic plugin host. Plugins implement this; the active adapter
/// (<c>BlueBeard.RocketMod</c> or <c>BlueBeard.OpenMod</c>) hosts it inside its
/// own plugin entry point and forwards Load/Unload.
/// </summary>
public interface IPluginHost
{
    /// <summary>Plugin name (used for logging and command-source attribution).</summary>
    string Name { get; }

    /// <summary>Filesystem directory where this plugin reads / writes its data.</summary>
    string Directory { get; }

    /// <summary>Commands exposed by this plugin. Adapters register them with the host framework.</summary>
    IReadOnlyList<ICommand> Commands { get; }

    void Load();
    void Unload();
}

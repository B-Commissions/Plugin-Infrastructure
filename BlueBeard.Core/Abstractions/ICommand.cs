using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Framework-agnostic command. Adapters wrap this in their host-specific command type
/// (<c>IRocketCommand</c> or <c>OpenMod.Core.Commands.Command</c>). Execution is async to
/// match OpenMod natively; the RocketMod adapter blocks on the result on a worker.
/// </summary>
public interface ICommand
{
    string Name { get; }
    string Help { get; }
    string Syntax { get; }
    IReadOnlyList<string> Aliases { get; }
    IReadOnlyList<string> Permissions { get; }

    /// <summary>True if the server console may invoke this command.</summary>
    bool AllowConsole { get; }

    /// <summary>True if a player may invoke this command.</summary>
    bool AllowPlayer { get; }

    Task ExecuteAsync(ICommandContext context);
}

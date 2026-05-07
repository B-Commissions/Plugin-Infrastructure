using System.Collections.Generic;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Per-invocation command context. Adapters construct one of these from RocketMod's
/// <c>(IRocketPlayer, string[])</c> pair or OpenMod's <c>ICommandContext</c>.
/// </summary>
public interface ICommandContext
{
    IPlayer Caller { get; }
    string CommandName { get; }
    IReadOnlyList<string> Args { get; }
}

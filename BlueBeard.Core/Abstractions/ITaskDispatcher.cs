using System;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Framework-agnostic main-thread dispatcher. RocketMod adapter uses
/// <c>Rocket.Core.Utils.TaskDispatcher</c>; OpenMod adapter uses its
/// <c>IUnturnedDispatcher</c>.
/// </summary>
public interface ITaskDispatcher
{
    /// <summary>Queue an action to run on the Unturned main thread.</summary>
    void QueueOnMainThread(Action action, float delaySeconds = 0);
}

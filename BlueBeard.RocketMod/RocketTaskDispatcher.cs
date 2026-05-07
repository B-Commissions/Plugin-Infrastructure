using System;
using BlueBeard.Core.Abstractions;
using Rocket.Core.Utils;

namespace BlueBeard.RocketMod;

internal sealed class RocketTaskDispatcher : ITaskDispatcher
{
    public void QueueOnMainThread(Action action, float delaySeconds = 0)
    {
        if (action == null) return;
        TaskDispatcher.QueueOnMainThread(action, delaySeconds);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Rocket.Core.Logging;
using Rocket.Core.Utils;

namespace BlueBeard.Core.Helpers;

public class ThreadHelper
{
    public static void RunAsynchronously(Action action, string exceptionMessage = null)
    {
        ThreadPool.QueueUserWorkItem((_) =>
        {
            try
            {
                action.Invoke();
            } catch (Exception e)
            {
                RunSynchronously(() => Logger.LogException(e, exceptionMessage));
            }
        });
    }

    public static void RunAsynchronously(Func<Task> asyncAction, string exceptionMessage = null)
    {
        Task.Run(async () =>
        {
            try
            {
                await asyncAction();
            } catch (Exception e)
            {
                RunSynchronously(() => Logger.LogException(e, exceptionMessage));
            }
        });
    }

    public static void RunSynchronously(Action action, float delaySeconds = 0)
    {
        TaskDispatcher.QueueOnMainThread(action, delaySeconds);
    }
}

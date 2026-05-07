using System;
using BlueBeard.Core.Abstractions;
using RocketLog = Rocket.Core.Logging.Logger;

namespace BlueBeard.RocketMod;

internal sealed class RocketLogger : ILogger
{
    public void Log(string message) => RocketLog.Log(message);
    public void LogWarning(string message) => RocketLog.LogWarning(message);
    public void LogError(string message) => RocketLog.LogError(message);
    public void LogException(Exception exception, string context = null) => RocketLog.LogException(exception, context);
}

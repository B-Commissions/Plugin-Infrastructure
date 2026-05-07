using System;

namespace BlueBeard.Core.Abstractions;

/// <summary>
/// Framework-agnostic logging interface. Adapters route to RocketMod's static
/// <c>Rocket.Core.Logging.Logger</c> or OpenMod's injected <c>ILogger&lt;T&gt;</c>.
/// </summary>
public interface ILogger
{
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogException(Exception exception, string context = null);
}

using System;
using BlueBeard.Core.Abstractions;
using MsLogger = Microsoft.Extensions.Logging.ILogger;
using MsLoggerExt = Microsoft.Extensions.Logging.LoggerExtensions;

namespace BlueBeard.OpenMod;

/// <summary>
/// <see cref="ILogger"/> adapter that delegates to a Microsoft.Extensions.Logging
/// <c>ILogger</c> instance — typically <c>ILogger&lt;TPlugin&gt;</c> injected by OpenMod's DI.
/// </summary>
public sealed class OpenModLogger : ILogger
{
    private readonly MsLogger _inner;

    public OpenModLogger(MsLogger inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public void Log(string message) => MsLoggerExt.LogInformation(_inner, message);
    public void LogWarning(string message) => MsLoggerExt.LogWarning(_inner, message);
    public void LogError(string message) => MsLoggerExt.LogError(_inner, message);

    public void LogException(Exception exception, string context = null)
    {
        if (string.IsNullOrEmpty(context))
            MsLoggerExt.LogError(_inner, exception, exception?.Message ?? "<no message>");
        else
            MsLoggerExt.LogError(_inner, exception, context);
    }
}

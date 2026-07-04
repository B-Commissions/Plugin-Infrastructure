using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace BlueBeard.Database;

/// <summary>
/// Transient-fault retry for single-statement, non-transactional operations: MySQL
/// deadlocks (1213) and lock-wait timeouts (1205) roll the statement back atomically,
/// so re-executing is safe. Operations inside a caller-owned <see cref="BbTransaction"/>
/// are never retried — a deadlock rolls back the whole transaction, which only the
/// caller can restart.
/// </summary>
internal static class DbRetry
{
    private const int MaxAttempts = 3;

    public static async Task<TResult> RunAsync<TResult>(Func<Task<TResult>> action)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (MySqlException ex) when (attempt < MaxAttempts && IsTransient(ex))
            {
                // 50ms, 200ms — long enough for the competing lock holder to finish,
                // short enough not to stall a game-server startup path.
                await Task.Delay(TimeSpan.FromMilliseconds(50 * Math.Pow(4, attempt - 1)));
            }
        }
    }

    public static async Task RunAsync(Func<Task> action)
    {
        await RunAsync(async () => { await action(); return true; });
    }

    private static bool IsTransient(MySqlException ex) =>
        ex.ErrorCode is MySqlErrorCode.LockDeadlock or MySqlErrorCode.LockWaitTimeout
        || ex.IsTransient;
}

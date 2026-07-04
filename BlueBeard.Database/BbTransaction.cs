using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace BlueBeard.Database;

/// <summary>
/// A database transaction owning its connection. Obtain via
/// <see cref="DatabaseManager.BeginTransactionAsync"/>; pass to the DbSet overloads that
/// accept a transaction so multiple writes commit or roll back atomically.
///
/// <code>
/// using var tx = await db.BeginTransactionAsync();
/// await db.Table&lt;Wallet&gt;().UpdateAsync(from, tx);
/// await db.Table&lt;Wallet&gt;().UpdateAsync(to, tx);
/// await tx.CommitAsync();
/// </code>
///
/// Disposing without committing rolls back.
/// </summary>
public sealed class BbTransaction : IDisposable
{
    public MySqlConnection Connection { get; }
    public MySqlTransaction Transaction { get; }

    private bool _completed;
    private bool _disposed;

    internal BbTransaction(MySqlConnection connection, MySqlTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public async Task CommitAsync()
    {
        if (_completed) throw new InvalidOperationException("Transaction already completed.");
        await Transaction.CommitAsync();
        _completed = true;
    }

    public async Task RollbackAsync()
    {
        if (_completed) throw new InvalidOperationException("Transaction already completed.");
        await Transaction.RollbackAsync();
        _completed = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (!_completed) Transaction.Rollback();
        }
        catch
        {
            // Rollback on a dead connection is best-effort; the server discards the
            // transaction when the connection drops anyway.
        }
        finally
        {
            Transaction.Dispose();
            Connection.Dispose();
        }
    }
}

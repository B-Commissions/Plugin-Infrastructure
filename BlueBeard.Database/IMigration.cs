using System.Threading.Tasks;
using MySqlConnector;

namespace BlueBeard.Database;

/// <summary>
/// A versioned, run-once migration step for changes automatic schema sync cannot express —
/// renames, backfills, data transforms, destructive changes.
///
/// Register via <see cref="DatabaseManager.RegisterMigration"/>. Applied versions are
/// tracked in the <c>__bluebeard_migrations</c> table; pending migrations run in ascending
/// <see cref="Version"/> order during <see cref="DatabaseManager.Load"/>, after schema sync.
///
/// <code>
/// public class RenameKillsColumn : IMigration
/// {
///     public int Version => 2;
///     public async Task UpAsync(MySqlConnection conn)
///     {
///         using var cmd = new MySqlCommand(
///             "ALTER TABLE `players` RENAME COLUMN `kils` TO `kills`;", conn);
///         await cmd.ExecuteNonQueryAsync();
///     }
/// }
/// </code>
/// </summary>
public interface IMigration
{
    /// <summary>Monotonic version number, unique per plugin database. Runs in ascending order.</summary>
    int Version { get; }

    Task UpAsync(MySqlConnection connection);
}

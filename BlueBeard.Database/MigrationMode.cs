namespace BlueBeard.Database;

/// <summary>
/// Controls how schema sync handles existing tables on every <see cref="DatabaseManager.Load"/>.
/// </summary>
public enum MigrationMode
{
    /// <summary>
    /// CREATE TABLE IF NOT EXISTS only. Existing tables are never altered. Default behavior.
    /// </summary>
    None,

    /// <summary>
    /// Add missing columns and modify columns whose SQL type has changed. Never drops columns
    /// (data preservation). Foreign keys are emitted on initial table creation but not added
    /// to existing tables — schema drift on FKs requires a manual ALTER.
    ///
    /// Type changes are attempted in-place; MySQL will refuse if existing data isn't coercible.
    /// </summary>
    Update,

    /// <summary>
    /// DROP TABLE then CREATE on every load. DEV ONLY — destroys all data on every restart.
    /// </summary>
    Reset
}

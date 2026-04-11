using System;
using BlueBeard.Database.Attributes;

namespace BlueBeard.Cooldowns;

/// <summary>
/// Row entity used by <see cref="PersistentCooldownManager"/> to persist cooldowns in MySQL.
/// Registered via <c>databaseManager.RegisterEntity&lt;BBCooldownRow&gt;()</c> before <c>Load()</c>.
/// </summary>
[Table("bb_cooldowns")]
public class BBCooldownRow
{
    [PrimaryKey]
    [Column("cooldown_key")]
    [ColumnType("VARCHAR(191)")]
    public string Key { get; set; }

    [Column("expiry")]
    [ColumnType("DATETIME")]
    public DateTime Expiry { get; set; }
}

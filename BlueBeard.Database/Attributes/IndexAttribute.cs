using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// Adds a secondary index on the column, named <c>ix_{table}_{column}</c> (or
/// <c>ix_{table}_{group}</c> for composites). Set <see cref="Group"/> on multiple
/// properties with the same name to build a composite index; <see cref="Order"/>
/// controls the column position within the composite (lower first).
///
/// Emitted on table creation; under <see cref="MigrationMode.Update"/> missing indexes
/// are created on existing tables. Indexes are matched by name and never dropped.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class IndexAttribute : Attribute
{
    /// <summary>Make this a UNIQUE index.</summary>
    public bool Unique { get; set; }

    /// <summary>Composite group name. Properties sharing a group form one index.</summary>
    public string Group { get; set; }

    /// <summary>Column position within a composite group (lower first). Ties break on declaration order.</summary>
    public int Order { get; set; }
}

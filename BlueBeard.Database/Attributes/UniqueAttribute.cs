using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// Enforces a single-column UNIQUE constraint, implemented as a unique index named
/// <c>ux_{table}_{column}</c>. Emitted on table creation; under
/// <see cref="MigrationMode.Update"/> missing unique indexes are created on existing
/// tables (never dropped).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class UniqueAttribute : Attribute { }

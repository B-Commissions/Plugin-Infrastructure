using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// Marks a column as NOT NULL in the database schema. Without this attribute (or
/// <see cref="ColumnAttribute.Nullable"/>) columns keep the historical default of
/// being nullable, so existing entities produce identical schemas.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RequiredAttribute : Attribute { }

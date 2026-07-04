using System;

namespace BlueBeard.Database.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;

    private bool _nullable = true;

    /// <summary>
    /// Explicit nullability: <c>[Column("x", Nullable = false)]</c> emits NOT NULL,
    /// <c>Nullable = true</c> emits NULL. Unset keeps the historical default (nullable,
    /// with no nullability clause in DDL) so existing entities are unaffected.
    /// <see cref="RequiredAttribute"/> is the equivalent of <c>Nullable = false</c> and wins on conflict.
    /// </summary>
    public bool Nullable
    {
        get => _nullable;
        set { _nullable = value; NullableExplicit = value; }
    }

    /// <summary>Tri-state view of <see cref="Nullable"/>: null when never assigned.</summary>
    public bool? NullableExplicit { get; private set; }
}

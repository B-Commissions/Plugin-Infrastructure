using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// Marks a property as a many-to-one navigation. Auto-populated whenever the child entity
/// is loaded, by querying the parent table using the local key value.
///
/// Loaded with a single batched <c>WHERE pk IN (...)</c> query covering all children
/// in the result set.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class BelongsToAttribute(string localKeyProperty) : Attribute
{
    /// <summary>
    /// The property name on this entity that holds the foreign key value.
    /// </summary>
    public string LocalKeyProperty { get; } = localKeyProperty;
}

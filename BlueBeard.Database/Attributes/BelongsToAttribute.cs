using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// Marks a property as a many-to-one navigation. Auto-populated whenever the child entity
/// is loaded, by querying the parent table using the local key value.
///
/// Loaded with a single batched <c>WHERE pk IN (...)</c> query covering all children
/// in the result set.
///
/// The parameterless form resolves the local foreign-key column via its
/// [ForeignKey(typeof(Parent))] type token, which survives obfuscation; the string form
/// names the local property directly (unobfuscated builds only).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class BelongsToAttribute : Attribute
{
    /// <summary>
    /// The property name on this entity that holds the foreign key value.
    /// Null when using type-token resolution.
    /// </summary>
    public string LocalKeyProperty { get; }

    /// <summary>
    /// Resolve the local FK column via its [ForeignKey] type token — durable under
    /// obfuscation. Requires exactly one FK on this type pointing at the parent entity.
    /// </summary>
    public BelongsToAttribute() { }

    public BelongsToAttribute(string localKeyProperty) => LocalKeyProperty = localKeyProperty;
}

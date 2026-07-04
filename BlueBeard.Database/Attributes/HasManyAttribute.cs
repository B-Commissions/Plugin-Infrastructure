using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// Marks a List&lt;T&gt; (or IList&lt;T&gt; / ICollection&lt;T&gt; / IEnumerable&lt;T&gt;) property
/// as a one-to-many navigation. Auto-populated whenever the parent entity is loaded.
///
/// The collection is loaded with a single batched <c>WHERE fk IN (...)</c> query covering
/// all parents in the result set — not one query per parent.
///
/// The parameterless form resolves the child's foreign-key column via its
/// [ForeignKey(typeof(Parent))] type token, which survives obfuscation; the string form
/// names the child property directly (unobfuscated builds only).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class HasManyAttribute : Attribute
{
    /// <summary>
    /// The property name on the related (child) entity that holds the foreign key
    /// pointing back to this entity's primary key. Null when using type-token resolution.
    /// </summary>
    public string ForeignKeyProperty { get; }

    /// <summary>
    /// Resolve the child's FK column via its [ForeignKey] type token — durable under
    /// obfuscation. Requires exactly one FK on the child type pointing at this entity.
    /// </summary>
    public HasManyAttribute() { }

    public HasManyAttribute(string foreignKeyProperty) => ForeignKeyProperty = foreignKeyProperty;
}

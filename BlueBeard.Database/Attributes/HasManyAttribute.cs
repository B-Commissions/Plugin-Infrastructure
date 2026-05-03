using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// Marks a List&lt;T&gt; (or IList&lt;T&gt; / ICollection&lt;T&gt; / IEnumerable&lt;T&gt;) property
/// as a one-to-many navigation. Auto-populated whenever the parent entity is loaded.
///
/// The collection is loaded with a single batched <c>WHERE fk IN (...)</c> query covering
/// all parents in the result set — not one query per parent.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class HasManyAttribute(string foreignKeyProperty) : Attribute
{
    /// <summary>
    /// The property name on the related (child) entity that holds the foreign key
    /// pointing back to this entity's primary key.
    /// </summary>
    public string ForeignKeyProperty { get; } = foreignKeyProperty;
}

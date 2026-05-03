using System;

namespace BlueBeard.Database.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : Attribute
{
    public Type ReferencedType { get; }
    public string ReferencedProperty { get; }
    public ReferentialAction OnDelete { get; set; } = ReferentialAction.Restrict;
    public ReferentialAction OnUpdate { get; set; } = ReferentialAction.Restrict;

    /// <param name="referencedType">The entity type this column references.</param>
    /// <param name="referencedProperty">The property name on the referenced type (typically the primary key).</param>
    public ForeignKeyAttribute(Type referencedType, string referencedProperty)
    {
        ReferencedType = referencedType;
        ReferencedProperty = referencedProperty;
    }
}

public enum ReferentialAction
{
    Restrict,
    Cascade,
    SetNull,
    NoAction
}

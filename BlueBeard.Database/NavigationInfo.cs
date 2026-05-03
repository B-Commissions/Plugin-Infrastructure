using System;
using System.Reflection;

namespace BlueBeard.Database;

public enum NavigationKind
{
    HasMany,
    BelongsTo
}

public class NavigationInfo
{
    public PropertyInfo PropertyInfo { get; set; }
    public NavigationKind Kind { get; set; }

    /// <summary>
    /// HasMany: the element type T (from List&lt;T&gt;).
    /// BelongsTo: the property type itself (the parent entity type).
    /// </summary>
    public Type ElementType { get; set; }

    /// <summary>
    /// HasMany: the property name on the related (child) entity holding the FK back to this entity's PK.
    /// </summary>
    public string ForeignKeyProperty { get; set; }

    /// <summary>
    /// BelongsTo: the property name on this entity holding the FK to the parent.
    /// </summary>
    public string LocalKeyProperty { get; set; }
}

using System;

namespace BlueBeard.Core.Validation;

/// <summary>
/// Numeric range constraint (inclusive). On correction, out-of-range values are clamped
/// to the nearest bound.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RangeAttribute(double min, double max) : Attribute
{
    public double Min { get; } = min;
    public double Max { get; } = max;
}

/// <summary>Inclusive numeric lower bound. Corrected by clamping.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class MinValueAttribute(double min) : Attribute
{
    public double Min { get; } = min;
}

/// <summary>Inclusive numeric upper bound. Corrected by clamping.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class MaxValueAttribute(double max) : Attribute
{
    public double Max { get; } = max;
}

/// <summary>
/// Requires a non-null, non-whitespace string or a non-empty collection.
/// Corrected by resetting from the defaults instance.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotEmptyAttribute : Attribute { }

/// <summary>
/// Requires a string to match the given regular expression. Null strings pass —
/// combine with [NotEmpty] to require presence. Corrected by resetting from defaults.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RegexMatchAttribute(string pattern) : Attribute
{
    public string Pattern { get; } = pattern;
}

/// <summary>
/// Restricts the value to a fixed set (compared case-insensitively on the string form,
/// so enums, strings, and numbers all behave sensibly in hand-edited config files).
/// Corrected by resetting from defaults.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OneOfAttribute(params object[] allowed) : Attribute
{
    public object[] Allowed { get; } = allowed;
}

/// <summary>
/// Recurse validation into a nested object, or element-wise into a list of objects.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateNestedAttribute : Attribute { }

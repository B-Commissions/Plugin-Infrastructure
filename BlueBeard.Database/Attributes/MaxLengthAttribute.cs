using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// Controls the SQL sizing of a string column: <c>[MaxLength(64)]</c> → VARCHAR(64),
/// <c>[MaxLength(Text = true)]</c> (or a length above 16383) → TEXT. Without the
/// attribute strings keep the historical VARCHAR(255) default.
/// Ignored when <see cref="ColumnTypeAttribute"/> overrides the SQL type outright.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MaxLengthAttribute : Attribute
{
    /// <summary>Maximum VARCHAR length above which the column is emitted as TEXT instead.</summary>
    public const int VarcharLimit = 16383;

    public int Length { get; }

    /// <summary>Emit the column as TEXT regardless of length.</summary>
    public bool Text { get; set; }

    public MaxLengthAttribute() { }

    public MaxLengthAttribute(int length)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length), "MaxLength must be positive.");
        Length = length;
    }
}

using System;

namespace BlueBeard.Database.Attributes;

/// <summary>
/// Server-side defaults that have no CLR literal representation.
/// </summary>
public enum ServerDefault
{
    /// <summary>DEFAULT CURRENT_TIMESTAMP — for DATETIME/TIMESTAMP columns.</summary>
    CurrentTimestamp
}

/// <summary>
/// Declares a database-side DEFAULT for the column, emitted in CREATE/MODIFY DDL.
/// Affects rows inserted without the column (raw SQL, other tools); the ORM's own
/// <c>InsertAsync</c> always sends every mapped value and is unaffected.
/// Supports numeric, string, bool, and enum literals, plus <see cref="ServerDefault"/>
/// for expressions like CURRENT_TIMESTAMP.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DefaultValueAttribute : Attribute
{
    public object Value { get; }
    public ServerDefault? ServerDefault { get; }

    // Typed overloads exist so the literal 0 binds to int rather than silently
    // converting to the ServerDefault enum (C# permits implicit 0 -> enum).
    public DefaultValueAttribute(int value) => Value = value;
    public DefaultValueAttribute(long value) => Value = value;
    public DefaultValueAttribute(double value) => Value = value;
    public DefaultValueAttribute(bool value) => Value = value;
    public DefaultValueAttribute(string value) => Value = value;
    public DefaultValueAttribute(object value) => Value = value;

    public DefaultValueAttribute(ServerDefault serverDefault) => ServerDefault = serverDefault;
}

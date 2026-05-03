using System;

namespace BlueBeard.Database.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnTypeAttribute(string sqlType) : Attribute
{
    public string SqlType { get; } = sqlType;
}

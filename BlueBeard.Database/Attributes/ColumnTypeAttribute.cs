using System;

namespace BlueBeard.Database.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnTypeAttribute : Attribute
{
    public string SqlType { get; }
    public ColumnTypeAttribute(string sqlType) { SqlType = sqlType; }
}

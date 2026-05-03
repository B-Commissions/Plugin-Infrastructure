using System;

namespace BlueBeard.Database.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ColumnConverterAttribute(Type converterType) : Attribute
{
    public Type ConverterType { get; } = converterType;
}
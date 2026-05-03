using System;

namespace BlueBeard.Database.Converters;

public interface IValueConverter
{
    Type ClrType { get; }
    string DefaultSqlType { get; }
    object ToProvider(object clrValue);
    object FromProvider(object providerValue);
}
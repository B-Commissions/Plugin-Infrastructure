using System;

namespace BlueBeard.Database.Converters;

public class ByteArrayConverter : IValueConverter
{
    public Type ClrType => typeof(byte[]);
    public string DefaultSqlType => "VARBINARY(255)";
    public object ToProvider(object v) => v;
    public object FromProvider(object v) => v switch
    {
        byte[] b => b,
        string s => Convert.FromBase64String(s),
        _ => throw new InvalidCastException(
            $"Cannot convert {v?.GetType().Name ?? "null"} to byte[]")
    };
}
using System;

namespace BlueBeard.Database.Converters;

public class GuidBinaryConverter : IValueConverter
{
    // Not auto-registered — users opt in with [ColumnConverter(typeof(GuidBinaryConverter))]
    public Type ClrType => typeof(Guid);
    public string DefaultSqlType => "BINARY(16)";
    public object ToProvider(object v) => ((Guid)v).ToByteArray();
    public object FromProvider(object v) => v switch
    {
        byte[] { Length: 16 } b => new Guid(b),
        Guid g => g,
        string s => Guid.Parse(s),
        _ => throw new InvalidCastException($"Cannot convert {v?.GetType().Name ?? "null"} to Guid")
    };
}
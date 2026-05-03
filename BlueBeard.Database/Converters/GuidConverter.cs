using System;

namespace BlueBeard.Database.Converters;

public class GuidConverter : IValueConverter
{
    public Type ClrType => typeof(Guid);
    public string DefaultSqlType => "CHAR(36)";
    public object ToProvider(object v) => ((Guid)v).ToString();
    public object FromProvider(object v) => v switch
    {
        Guid g => g,
        string s => Guid.Parse(s),
        byte[] b when b.Length == 16 => new Guid(b),
        _ => throw new InvalidCastException(
            $"Cannot convert {v?.GetType().Name ?? "null"} to Guid")
    };
}
using System;
using System.Collections.Concurrent;

namespace BlueBeard.Database.Converters;

public static class ValueConverters
{
    private static readonly ConcurrentDictionary<Type, IValueConverter> _byClrType = new();

    static ValueConverters()
    {
        // Built-ins registered by default
        Register(new GuidConverter());
        Register(new ByteArrayConverter());
        Register(new TimeSpanConverter());
    }

    public static void Register(IValueConverter c) => _byClrType[c.ClrType] = c;

    public static bool TryGet(Type clrType, out IValueConverter c) =>
        _byClrType.TryGetValue(Nullable.GetUnderlyingType(clrType) ?? clrType, out c);
}

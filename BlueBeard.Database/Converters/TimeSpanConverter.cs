using System;

namespace BlueBeard.Database.Converters;

public class TimeSpanConverter : IValueConverter
{
    public Type ClrType => typeof(TimeSpan);
    public string DefaultSqlType => "BIGINT"; // ticks
    public object ToProvider(object v) => ((TimeSpan)v).Ticks;
    public object FromProvider(object v) => new TimeSpan(Convert.ToInt64(v));
}
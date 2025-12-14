using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TuneShelf.Converters;

public sealed class DurationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
        }

        if (value is long l)
        {
            var ts = TimeSpan.FromSeconds(l);
            return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

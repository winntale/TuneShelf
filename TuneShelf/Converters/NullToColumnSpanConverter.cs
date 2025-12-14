using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TuneShelf.Converters;

public class NullToColumnSpanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? 3 : 1;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

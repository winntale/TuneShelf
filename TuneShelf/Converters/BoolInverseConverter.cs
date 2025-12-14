using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TuneShelf.Converters;

public sealed class BoolInverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        throw new NotSupportedException();
    }
}

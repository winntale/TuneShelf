using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TuneShelf.Converters;

public sealed class IndexToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int idx && parameter is string s && int.TryParse(s, out var target))
            return idx == target;

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

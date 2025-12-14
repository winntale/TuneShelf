using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TuneShelf.Converters;

public sealed class IndexToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is null) return Brushes.Transparent;
        if (!int.TryParse(parameter.ToString(), out var idx)) return Brushes.Transparent;
        if (value is int sel && sel == idx)
            return new SolidColorBrush(Color.Parse("#007ACC"));

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

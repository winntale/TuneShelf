using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TuneShelf.Converters;

public sealed class IndexToForegroundConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var defaultText = new SolidColorBrush(Color.Parse("#E0E0E0"));

		if (parameter is null) return defaultText;
		if (!int.TryParse(parameter.ToString(), out var idx)) return defaultText;
		if (value is int sel && sel == idx)
			return Brushes.White;

		return defaultText;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}


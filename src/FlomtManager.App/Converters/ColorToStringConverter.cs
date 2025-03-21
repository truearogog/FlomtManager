using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FlomtManager.App.Extensions;

namespace FlomtManager.App.Converters;

public class ColorToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Color.TryParse((string)value, out var color) ? color : Colors.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is Color color ? color : Colors.White).ToStringARGB();
    }
}

using Avalonia.Data.Converters;
using System.Globalization;

namespace FlomtManager.App.Converters;

public class NotEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(value?.Equals(parameter)) ?? false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return true;
    }
}

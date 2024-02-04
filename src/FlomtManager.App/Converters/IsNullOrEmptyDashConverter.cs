using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FlomtManager.App.Converters
{
    public class IsNullOrEmptyDashConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty((string?)value) ? "-" : value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (string?)value == "-" ? string.Empty : value;
        }
    }
}

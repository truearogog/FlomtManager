using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FlomtManager.App.Converters
{
    public class IsNullOrEmptyElseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty((string?)value) ? parameter : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string?)value == (string?)parameter ? string.Empty : value;
        }
    }
}

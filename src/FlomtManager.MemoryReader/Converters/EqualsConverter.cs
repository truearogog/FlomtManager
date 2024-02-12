using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FlomtManager.MemoryReader.Converters
{
    public class EqualsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.Equals(parameter) ?? false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return true;
        }
    }
}

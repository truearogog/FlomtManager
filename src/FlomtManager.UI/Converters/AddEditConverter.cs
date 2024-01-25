using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FlomtManager.UI.Converters
{
    public class AddEditConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value == 1? "Edit" : "Add";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value == "Edit" ? 1 : 0;
        }
    }
}

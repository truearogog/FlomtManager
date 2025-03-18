using System.Globalization;
using Avalonia.Data.Converters;

namespace FlomtManager.App.Converters
{
    public class AddEditConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int?)value > 0 ? "Edit" : "Add";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value == "Edit" ? 1 : 0;
        }
    }
}

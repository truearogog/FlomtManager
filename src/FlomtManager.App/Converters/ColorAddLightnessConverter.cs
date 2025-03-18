using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FlomtManager.Framework.Skia.Extensions;
using SkiaSharp;

namespace FlomtManager.App.Converters
{
    public class ColorAddLightnessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = SKColor.Parse((value as string) ?? throw new InvalidCastException());
            var lightness = float.Parse((parameter as string) ?? throw new InvalidCastException());
            return new SolidColorBrush(Color.Parse(color.AddLightness(lightness).ToString()));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

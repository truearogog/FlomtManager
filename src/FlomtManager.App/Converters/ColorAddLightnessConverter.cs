using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
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
            var resultColor = color.AddLightness(lightness).ToString();
            return new ImmutableSolidColorBrush(Color.Parse(resultColor));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

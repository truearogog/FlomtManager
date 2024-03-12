using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using SkiaSharp;

namespace FlomtManager.App.Extensions
{
    public static class ApplicationExtensions
    {
        public static bool TryGetResource<T>(this Application application, string resourceKey, ThemeVariant themeVariant, out T? resource)
        {
            if (!application.TryGetResource(resourceKey, themeVariant, out var _resource))
            {
                resource = default;
                return false;
            }

            resource = (T?)_resource;
            return true;
        }

        public static SolidColorBrush GetBrushResource(this Application application, string resourceKey, ThemeVariant themeVariant)
        {
            if (!application!.TryGetResource<SolidColorBrush>(resourceKey, themeVariant, out var brush))
            {
                throw new InvalidOperationException($"{resourceKey} for requested theme not found!");
            }
            return brush ?? throw new InvalidCastException();
        }

        public static Color GetColorResource(this Application application, string resourceKey, ThemeVariant themeVariant)
        {
            if (!application!.TryGetResource<Color>(resourceKey, themeVariant, out var color))
            {
                throw new InvalidOperationException($"{resourceKey} for requested theme not found!");
            }
            return color;
        }
    }
}

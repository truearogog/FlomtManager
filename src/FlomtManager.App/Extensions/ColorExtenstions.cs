using System.Globalization;
using Avalonia.Media;

namespace FlomtManager.App.Extensions;

internal static class ColorExtenstions
{
    public static string ToStringARGB(this Color color)
    {
        return $"#{color.ToUInt32().ToString("x8", CultureInfo.InvariantCulture)}";
    }
}

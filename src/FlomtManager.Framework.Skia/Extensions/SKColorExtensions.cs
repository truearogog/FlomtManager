using SkiaSharp;

namespace FlomtManager.Framework.Skia.Extensions
{
    public static class SKColorExtensions
    {
        public static SKColor AddLightness(this SKColor color, float lightness)
        {
            color.ToHsl(out var h, out var s, out var l);
            return SKColor.FromHsl(h, s, Math.Clamp(l + lightness, 0, 100));
        }
    }
}

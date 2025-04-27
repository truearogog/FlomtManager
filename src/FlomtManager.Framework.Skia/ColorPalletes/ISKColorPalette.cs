using SkiaSharp;

namespace FlomtManager.Framework.Skia.ColorPalletes;

public interface ISKColorPalette
{
    int ColorCount { get; }
    int ShadeCount { get; }
    SKColor GetColor(int colorIndex, int shadeIndex);
}

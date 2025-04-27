using SkiaSharp;

namespace FlomtManager.Framework.Skia.ColorPalletes;

public sealed class VibrantSKColorPalette : ISKColorPalette
{
    private readonly SKColor[,] _colors;

    public int ColorCount => 9; // 9 base colors
    public int ShadeCount => 5; // 5 shades per color

    public VibrantSKColorPalette()
    {
        _colors = GenerateVibrantColors();
    }

    public SKColor GetColor(int colorIndex, int shadeIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(colorIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(colorIndex, ColorCount);
        ArgumentOutOfRangeException.ThrowIfNegative(shadeIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(shadeIndex, ShadeCount);

        return _colors[colorIndex, shadeIndex];
    }

    private SKColor[,] GenerateVibrantColors()
    {
        var colors = new SKColor[ColorCount, ShadeCount];
        float[] hues = [0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f]; // Evenly spaced hues

        // Generate vibrant colors
        for (int colorIdx = 0; colorIdx < hues.Length; colorIdx++)
        {
            float hue = hues[colorIdx];
            for (int shadeIdx = 0; shadeIdx < ShadeCount; shadeIdx++)
            {
                // Vibrant: high saturation (100-60%), moderate lightness (70-30%)
                float saturation = 1.0f - (shadeIdx * 0.1f); // 100% to 60%  
                float lightness = 0.7f - (shadeIdx * 0.1f); // 70% to 30%
                colors[colorIdx, shadeIdx] = FromHsl(hue, saturation, lightness);
            }
        }

        // Add gray as the last color (index 8)
        for (int shadeIdx = 0; shadeIdx < ShadeCount; shadeIdx++)
        {
            // Gray: zero saturation, lightness from 70% to 30%
            float lightness = 0.7f - (shadeIdx * 0.1f); // 70% to 30%
            colors[8, shadeIdx] = FromHsl(0f, 0f, lightness); // Hue is irrelevant for gray
        }

        return colors;
    }

    private SKColor FromHsl(float hue, float saturation, float lightness)
    {
        // Convert HSL to RGB
        hue %= 360f;
        float c = (1f - Math.Abs(2f * lightness - 1f)) * saturation;
        float x = c * (1f - Math.Abs((hue / 60f) % 2f - 1f));
        float m = lightness - c / 2f;

        float r, g, b;
        if (hue < 60f)
        { r = c; g = x; b = 0f; }
        else if (hue < 120f)
        { r = x; g = c; b = 0f; }
        else if (hue < 180f)
        { r = 0f; g = c; b = x; }
        else if (hue < 240f)
        { r = 0f; g = x; b = c; }
        else if (hue < 300f)
        { r = x; g = 0f; b = c; }
        else
        { r = c; g = 0f; b = x; }

        byte red = (byte)((r + m) * 255);
        byte green = (byte)((g + m) * 255);
        byte blue = (byte)((b + m) * 255);

        return new SKColor(red, green, blue);
    }
}

using Avalonia.Controls;
using Avalonia.Media;

namespace FlomtManager.App.ColorPalettes;

public class VibrantColorPalette : IColorPalette
{
    private readonly Color[,] _colors;

    public VibrantColorPalette()
    {
        _colors = GenerateVibrantColors();
    }

    public int ColorCount => 8; // 8 base colors
    public int ShadeCount => 5; // 5 shades per color

    public Color GetColor(int colorIndex, int shadeIndex)
    {
        if (colorIndex < 0 || colorIndex >= ColorCount || shadeIndex < 0 || shadeIndex >= ShadeCount)
            throw new ArgumentOutOfRangeException();
        return _colors[colorIndex, shadeIndex];
    }

    private Color[,] GenerateVibrantColors()
    {
        var colors = new Color[ColorCount, ShadeCount];
        float[] hues = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f }; // Evenly spaced hues

        for (int colorIdx = 0; colorIdx < ColorCount; colorIdx++)
        {
            float hue = hues[colorIdx];
            for (int shadeIdx = 0; shadeIdx < ShadeCount; shadeIdx++)
            {
                // Vibrant: high saturation (60-100%), varied lightness (30-70%)
                float saturation = 0.6f + (shadeIdx * 0.1f); // 60% to 100%
                float lightness = 0.7f - (shadeIdx * 0.1f);  // 70% to 30%
                colors[colorIdx, shadeIdx] = FromHsl(hue, saturation, lightness);
            }
        }

        return colors;
    }

    private Color FromHsl(float hue, float saturation, float lightness)
    {
        hue %= 360f;
        float c = (1f - System.Math.Abs(2f * lightness - 1f)) * saturation;
        float x = c * (1f - System.Math.Abs((hue / 60f) % 2f - 1f));
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

        return Color.FromRgb(red, green, blue);
    }
}

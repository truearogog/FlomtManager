using Avalonia.Controls;
using Avalonia.Media;

namespace FlomtManager.App.ColorPalettes;

public class PastelColorPalette : IColorPalette
{
    private readonly Color[,] _colors;

    public PastelColorPalette()
    {
        _colors = GeneratePastelColors();
    }

    public int ColorCount => 8; // 8 base colors
    public int ShadeCount => 5; // 5 shades per color

    public Color GetColor(int colorIndex, int shadeIndex)
    {
        if (colorIndex < 0 || colorIndex >= ColorCount || shadeIndex < 0 || shadeIndex >= ShadeCount)
            throw new ArgumentOutOfRangeException();
        return _colors[colorIndex, shadeIndex];
    }

    private Color[,] GeneratePastelColors()
    {
        var colors = new Color[ColorCount, ShadeCount];
        float[] hues = [0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f]; // Evenly spaced hues

        for (int colorIdx = 0; colorIdx < ColorCount; colorIdx++)
        {
            float hue = hues[colorIdx];
            for (int shadeIdx = 0; shadeIdx < ShadeCount; shadeIdx++)
            {
                // Pastel: low saturation (20-40%), high lightness (60-80%)
                float saturation = 0.2f + (shadeIdx * 0.05f); // 20% to 40%
                float lightness = 0.8f - (shadeIdx * 0.05f);  // 80% to 60%
                colors[colorIdx, shadeIdx] = FromHsl(hue, saturation, lightness);
            }
        }

        return colors;
    }

    private Color FromHsl(float hue, float saturation, float lightness)
    {
        // Convert HSL to RGB
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

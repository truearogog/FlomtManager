using Avalonia.Controls;
using Avalonia.Media;
using FlomtManager.Framework.Skia.ColorPalletes;

namespace FlomtManager.App.ColorPalettes;

public class ColorPaletteAdapter(ISKColorPalette colorPalette) : IColorPalette
{
    public int ColorCount => colorPalette.ColorCount;
    public int ShadeCount => colorPalette.ShadeCount;

    public Color GetColor(int colorIndex, int shadeIndex)
    {
        var color = colorPalette.GetColor(colorIndex, shadeIndex);
        return Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
    }
}

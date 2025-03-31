using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace FlomtManager.App.Controls;

public partial class ColorPaletteButton : UserControl
{
    private readonly Button _button;
    private readonly ItemsControl _paletteItemsControl;
    private readonly Flyout _flyout;

    public static readonly DirectProperty<ColorPaletteButton, Color> SelectedColorProperty =
        AvaloniaProperty.RegisterDirect<ColorPaletteButton, Color>(
            nameof(SelectedColor),
            o => o.SelectedColor,
            (o, v) => o.SelectedColor = v,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<ColorPaletteButton, object> ButtonContentProperty =
        AvaloniaProperty.RegisterDirect<ColorPaletteButton, object>(
            nameof(ButtonContent),
            o => o.ButtonContent,
            (o, v) => o.ButtonContent = v);

    public static readonly DirectProperty<ColorPaletteButton, IColorPalette> ColorPaletteProperty =
        AvaloniaProperty.RegisterDirect<ColorPaletteButton, IColorPalette>(
            nameof(ColorPalette),
            o => o.ColorPalette,
            (o, v) => o.ColorPalette = v);

    private Color _selectedColor;
    private object _buttonContent;
    private IColorPalette _colorPalette;

    public ColorPaletteButton()
    {
        InitializeComponent();
        _button = this.FindControl<Button>("ColorButton");
        _paletteItemsControl = this.FindControl<ItemsControl>("ColorPaletteControl");
        _flyout = new Flyout
        {
            Content = _paletteItemsControl,
            Placement = PlacementMode.Bottom
        };
        _button.Flyout = _flyout;

        UpdateColorItems();
    }

    public Color SelectedColor
    {
        get => _selectedColor;
        set => SetAndRaise(SelectedColorProperty, ref _selectedColor, value);
    }

    public object ButtonContent
    {
        get => _buttonContent;
        set
        {
            SetAndRaise(ButtonContentProperty, ref _buttonContent, value);
            _button.Content = value;
        }
    }

    public IColorPalette ColorPalette
    {
        get => _colorPalette;
        set
        {
            SetAndRaise(ColorPaletteProperty, ref _colorPalette, value);
            UpdateColorItems();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _button.Content = ButtonContent;
        UpdateColorItems();
    }

    private void UpdateColorItems()
    {
        if (ColorPalette == null || _paletteItemsControl == null)
        {
            return;
        }

        // Populate colors
        var colors = new List<Color>();
        for (int shadeIdx = ColorPalette.ShadeCount - 1; shadeIdx >= 0; shadeIdx--)
        {
            for (int colorIdx = 0; colorIdx < ColorPalette.ColorCount; colorIdx++)
            {
                colors.Add(ColorPalette.GetColor(colorIdx, shadeIdx));
            }
        }
        _paletteItemsControl.ItemsSource = colors;
    }

    private void ColorItem_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Color color)
        {
            SelectedColor = color;
            _flyout.Hide();
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace FlomtManager.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ToggleButton_OnIsCheckedChanged(object sender, RoutedEventArgs e)
        {
            var app = Avalonia.Application.Current;
            if (app is not null)
            {
                var theme = app.ActualThemeVariant;
                app.RequestedThemeVariant = theme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
            }
        }
    }
}
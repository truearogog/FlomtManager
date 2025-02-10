using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlomtManager.MemoryReader.Models;
using FlomtManager.MemoryReader.ViewModels;
using FlomtManager.MemoryReader.Views;
using System.Text.Json;

namespace FlomtManager.MemoryReader
{
    public partial class App : Application
    {
        private static string FormSerializationPath => Path.Combine(Environment.CurrentDirectory, "serializedform.json");

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindowViewModel = new MainWindowViewModel();
                if (File.Exists(FormSerializationPath))
                {
                    mainWindowViewModel.Form = DeserializeForm();
                }

                var window = new MainWindow
                {
                    DataContext = mainWindowViewModel,
                };
                window.Closing += Window_Closing;
                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Window_Closing(object sender, Avalonia.Controls.WindowClosingEventArgs e)
        {
            if (sender is MainWindow mainWindow && mainWindow.DataContext is MainWindowViewModel mainWindowViewModel)
            {
                SerializeForm(mainWindowViewModel.Form);
            }
        }

        private static void SerializeForm(FormViewModel formViewModel)
        {
            var file = Path.Combine(Environment.CurrentDirectory, FormSerializationPath);
            var json = JsonSerializer.Serialize(formViewModel, FormViewModelContext.Default.FormViewModel);
            File.WriteAllText(file, json);
        }

        private static FormViewModel DeserializeForm()
        {
            var json = File.ReadAllText(FormSerializationPath);
            return JsonSerializer.Deserialize(json, FormViewModelContext.Default.FormViewModel) ?? new FormViewModel();
        }
    }
}
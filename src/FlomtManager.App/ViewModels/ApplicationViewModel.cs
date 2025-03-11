using Avalonia.Controls.ApplicationLifetimes;

namespace FlomtManager.App.ViewModels
{
    public partial class ApplicationViewModel : ViewModel
    {
        public ApplicationViewModel()
        {
        }

        public void TrayIconClicked()
        {
            var mainWindow = ((IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current!.ApplicationLifetime!).MainWindow!;
            mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            mainWindow.Show();
            mainWindow.Activate();
        }

        public static void Exit()
        {
            var appLifetime = (IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current!.ApplicationLifetime!;
            appLifetime.TryShutdown();
        }
    }
}

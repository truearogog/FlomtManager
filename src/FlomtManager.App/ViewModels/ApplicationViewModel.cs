using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace FlomtManager.App.ViewModels
{
    public partial class ApplicationViewModel : ViewModelBase
    {
        public ApplicationViewModel()
        {
        }

        public void TrayIconClicked()
        {
            var mainWindow = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
            mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            mainWindow.Show();
            mainWindow.Activate();
        }

        public static void Exit()
        {
            var appLifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
            appLifetime.TryShutdown();
        }
    }
}

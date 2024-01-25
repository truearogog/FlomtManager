using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using System.Windows.Input;

namespace FlomtManager.UI.ViewModels
{
    public partial class ApplicationViewModel : ViewModelBase
    {
        public ICommand TrayIconClickedCommand { get; set; }
        public ICommand ExitCommand { get; set; }

        public ApplicationViewModel()
        {
            TrayIconClickedCommand = ReactiveCommand.Create(TrayIconClicked);
            ExitCommand = ReactiveCommand.Create(Exit);
        }

        private void TrayIconClicked()
        {
            var mainWindow = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
            mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            mainWindow.Show();
            mainWindow.Activate();
        }

        private void Exit()
        {
            var appLifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
            appLifetime.TryShutdown();
        }
    }
}

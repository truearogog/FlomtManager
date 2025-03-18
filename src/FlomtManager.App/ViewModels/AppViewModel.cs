using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;

namespace FlomtManager.App.ViewModels;

internal sealed class AppViewModel : ReactiveObject
{
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

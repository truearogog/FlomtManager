using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlomtManager.App.Stores;
using FlomtManager.App.ViewModels;
using FlomtManager.App.Views;
using FlomtManager.Core;
using FlomtManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FlomtManager.App
{
    public partial class App : Application
    {
        public static IHost Host;

        public App()
        {
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services.AppAppEF(context.Configuration);
                    services.AddServices();

                    // register stores
                    services.AddSingleton<DeviceWindowStore>();
                    services.AddSingleton<DeviceStore>();

                    // register views and viewmodels
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<DevicesViewModel>();
                    services.AddTransient<DeviceViewModel>();
                    services.AddTransient<DeviceCreateUpdateViewModel>();
                    services.AddTransient<DeviceConnectionViewModel>();
                    services.AddTransient<DataGroupChartViewModel>();
                    services.AddTransient<DataGroupTableViewModel>();
                    services.AddTransient<DataGroupIntegrationViewModel>();
                })
                .Build();
            Host.Start();

            if (!Design.IsDesignMode)
            {
                var db = Host.Services.GetRequiredService<IAppDb>() as DbContext
                    ?? throw new InvalidOperationException($"{nameof(IAppDb)} must implement {nameof(DbContext)}.");
                db.Database.Migrate();
            }

            DataContext = new ApplicationViewModel();
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = Host.Services.GetRequiredService<MainWindow>();
                mainWindow.DataContext = Host.Services.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
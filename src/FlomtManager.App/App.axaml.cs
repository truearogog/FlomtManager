using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlomtManager.App.Stores;
using FlomtManager.App.ViewModels;
using FlomtManager.App.Views;
using FlomtManager.Application;
using FlomtManager.Core.Data;
using FlomtManager.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FlomtManager.App
{
    public partial class App : Avalonia.Application
    {
        public static IHost Host;

        public App()
        {
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    if (!Design.IsDesignMode)
                    {
                        services
                            .AppDatabase(context.Configuration)
                            .AddRepositories()
                            .AddServices()
                            .AddStores()
                            .AddApplication()
                            ;

                        services.AddSingleton<DeviceWindowStore>();

                        // register views and viewmodels
                        services.AddSingleton<MainWindow>();
                        services.AddSingleton<MainWindowViewModel>();
                        services.AddSingleton<DevicesViewModel>();
                        services.AddTransient<DeviceViewModel>();
                        services.AddTransient<DeviceCreateUpdateViewModel>();
                        services.AddTransient<DataGroupChartViewModel>();
                        services.AddTransient<DataGroupTableViewModel>();
                        services.AddTransient<DataGroupIntegrationViewModel>();
                    }
                })
                .Build();
            Host.Start();

            if (!Design.IsDesignMode)
            {
                var dbInitializer = Host.Services.GetRequiredService<IDbInitializer>();
                //dbInitializer.Drop().Wait(); // todo: remove
                //dbInitializer.Init().Wait();
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
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlomtManager.App.Stores;
using FlomtManager.App.ViewModels;
using FlomtManager.App.Views;
using FlomtManager.Application;
using FlomtManager.Domain.Abstractions.Data;
using FlomtManager.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FlomtManager.App
{
    public partial class App : Avalonia.Application
    {
        public static IServiceProvider Services;

        public App()
        {
            var host = Host.CreateDefaultBuilder()
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

                        services.AddSingleton<MainViewModel>();
                        services.AddSingleton<DeviceWindowStore>();
                    }
                })
                .Build();
            host.Start();

            Services = host.Services;

            if (!Design.IsDesignMode)
            {
                var dbInitializer = Services.GetRequiredService<IDbInitializer>();
                //dbInitializer.Drop().Wait(); // todo: remove
                dbInitializer.Init().Wait();
            }

            DataContext = new AppViewModel();
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>()
                };
                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
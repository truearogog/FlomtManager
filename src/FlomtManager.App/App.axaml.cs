using AutoMapper;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlomtManager.Data.EF;
using FlomtManager.Data.EF.Extensions;
using FlomtManager.Data.EF.SQLite;
using FlomtManager.Services.Extensions;
using FlomtManager.App.Profiles;
using FlomtManager.App.Stores;
using FlomtManager.App.ViewModels;
using FlomtManager.App.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;

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
                    services.AddAppEF<SQLiteAppDb>(options => options.UseSqlite(context.Configuration.GetConnectionString("AppDb")
                        ?? throw new InvalidOperationException("Connection string 'AppDb' not found.")), scope: ServiceLifetime.Transient);
                    services.AddServices();

                    // register stores
                    services.AddSingleton<DeviceStore>();

                    // register views and viewmodels
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<DevicesViewModel>();
                    services.AddTransient<DeviceViewModel>();
                    services.AddTransient<DeviceCreateUpdateViewModel>();

                    // register mapper
                    var config = new MapperConfiguration(cfg =>
                    {
                        cfg.AddProfile(typeof(DeviceProfile));
                    });
                    services.Add(new ServiceDescriptor(typeof(IMapper), new Mapper(config)));
                })
                .Build();
            Host.Start();

            var db = Host.Services.GetRequiredService<IAppDb>() as DbContext;
            db.Database.Migrate();

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
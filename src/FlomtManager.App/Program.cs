using Avalonia;
using Avalonia.Dialogs;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Serilog;
using Serilog.Templates;
using System;
using System.Reflection;
using System.Threading;

namespace FlomtManager.App
{
    internal class Program
    {
        private const int TimeoutSeconds = 3;

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            var mutex = new Mutex(false, typeof(Program).FullName);

            try
            {
                if (!mutex.WaitOne(TimeSpan.FromSeconds(TimeoutSeconds), true))
                {
                    return;
                }

                SetupLogger();
                Log.Information("Application starting up, v{Version}", Assembly.GetExecutingAssembly().GetName().Version!.ToString(3));

                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Application Main");
#if DEBUG
                throw;
#endif
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            IconProvider.Current
                .Register<FontAwesomeIconProvider>();

            return AppBuilder.Configure<App>()
                .UseManagedSystemDialogs()
                .UsePlatformDetect()
                .LogToTrace()
                // .With(new Win32PlatformOptions())
                .UseReactiveUI();
        }

        private static void SetupLogger()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", false).Build();
            var expressionTemplate =
                new ExpressionTemplate(
                    "[{@t:yyyy-MM-dd HH:mm:ss} {@l:u3} {Coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1), '<none>')}] {@m}\n{@x}");
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Debug(expressionTemplate)
                .WriteTo.File(expressionTemplate, "log/log.txt", rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 2, fileSizeLimitBytes: 10 * 1024 * 1024, rollOnFileSizeLimit: true)
                .CreateLogger();
        }
    }
}

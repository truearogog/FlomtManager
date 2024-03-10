using Avalonia;
using Avalonia.Dialogs;
using Avalonia.ReactiveUI;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace FlomtManager.MemoryReader
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
                .UseReactiveUI();
        }

        private static void SetupLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .WriteTo.Debug()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}

// src/QuanLyDKHP.App/Program.cs

using Avalonia;
using System;
using System.Threading.Tasks;
using QuanLyDKHP.Infrastructure.Services;

namespace QuanLyDKHP.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        CrashLogger.Info($"Ứng dụng khởi động (log: {CrashLogger.CurrentLogFilePath})");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            CrashLogger.Error("AppDomain.UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CrashLogger.Error("TaskScheduler.UnobservedTaskException", e.Exception);
            // Không SetObserved: giữ nguyên hành vi mặc định, chỉ bổ sung log.
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            CrashLogger.Error("StartWithClassicDesktopLifetime", ex);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}

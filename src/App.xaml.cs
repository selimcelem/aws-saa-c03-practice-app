using System.Diagnostics;

namespace AwsSaaC03Practice;

public partial class App : Application
{
    // Fixed path for easy access; fallback to AppDataDirectory
    private static readonly string _crashLogPath;

    static App()
    {
        try
        {
            // Use project directory if available (dev), otherwise app data
            var devPath = @"D:\Projects\aws-saa-c03-practice-app\crash.log";
            var dir = Path.GetDirectoryName(devPath);
            if (dir is not null && Directory.Exists(dir))
                _crashLogPath = devPath;
            else
                _crashLogPath = Path.Combine(FileSystem.AppDataDirectory, "crash.log");
        }
        catch
        {
            _crashLogPath = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "saa-c03-crash.log");
        }
    }

    public App(AppShell shell)
    {
        // Global exception handlers — register before InitializeComponent
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogCrash("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

#if WINDOWS
        Microsoft.Maui.MauiWinUIApplication.Current.UnhandledException += (_, e) =>
        {
            LogCrash("WinUI.UnhandledException", e.Exception);
            e.Handled = true;
        };
#endif

        InitializeComponent();
        MainPage = shell;
    }

    public static void LogCrash(string source, Exception? ex)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}: {ex}\n---\n";
        try
        {
            File.AppendAllText(_crashLogPath, entry);
        }
        catch { /* last resort — can't write to disk */ }
        Debug.WriteLine(entry);
        Console.WriteLine(entry);
    }
}

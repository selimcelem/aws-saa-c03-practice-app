using System.Diagnostics;

namespace AwsSaaC03Practice;

public partial class App : Application
{
    private static readonly string _crashLogPath =
        Path.Combine(FileSystem.AppDataDirectory, "crash.log");

    public App(AppShell shell)
    {
        InitializeComponent();
        MainPage = shell;

        // Global exception handlers
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogCrash("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
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
    }
}

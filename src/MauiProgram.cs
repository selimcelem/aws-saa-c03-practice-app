using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using AwsSaaC03Practice.Services;
using AwsSaaC03Practice.ViewModels;
using AwsSaaC03Practice.Views;

namespace AwsSaaC03Practice;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseLiveCharts()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-SemiBold.ttf", "OpenSansSemiBold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ── Services ─────────────────────────────────────────────────────────
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<QuestionService>();
        builder.Services.AddSingleton<SessionDbService>();
        builder.Services.AddSingleton<S3SyncService>();
        builder.Services.AddSingleton<ReportService>();

        // ── ViewModels ───────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ModePickerViewModel>();
        builder.Services.AddTransient<QuizViewModel>();
        builder.Services.AddTransient<ResultsViewModel>();

        // ── Views ────────────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ModePickerPage>();
        builder.Services.AddTransient<QuizPage>();
        builder.Services.AddTransient<ResultsPage>();

        // ── App shell ────────────────────────────────────────────────────────
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

        return builder.Build();

        // NOTE: No synchronous bootstrap here — services lazy-initialize on first access.
        // The previous Task.Run().GetAwaiter().GetResult() blocked the main thread during
        // WinUI3 composition initialization, causing intermittent exit code -1073741189.
    }
}

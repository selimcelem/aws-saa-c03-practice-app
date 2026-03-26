using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
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
            .UseLiveCharts()    // Includes SkiaSharp setup
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

        var app = builder.Build();

        // ── Bootstrap: load settings and question bank before UI starts ──────
        var settings  = app.Services.GetRequiredService<SettingsService>();
        var questions = app.Services.GetRequiredService<QuestionService>();

        // Run bootstrap synchronously on the main thread — app is not yet shown
        Task.Run(async () =>
        {
            await settings.LoadAsync();
            await questions.LoadAsync();
        }).GetAwaiter().GetResult();

        return app;
    }
}

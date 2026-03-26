using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AwsSaaC03Practice.Models;
using AwsSaaC03Practice.Services;

namespace AwsSaaC03Practice.ViewModels;

[QueryProperty(nameof(SessionIdParam), "sessionId")]
public partial class ResultsViewModel : BaseViewModel
{
    private readonly SessionDbService _db;
    private readonly S3SyncService _s3;
    private readonly AuthService _auth;
    private SessionResult? _result;

    [ObservableProperty] private string _sessionIdParam = "";
    [ObservableProperty] private string _scoreText = "";
    [ObservableProperty] private string _correctText = "";
    [ObservableProperty] private string _timeText = "";
    [ObservableProperty] private string _avgTimeText = "";
    [ObservableProperty] private List<CategoryScore> _needsWork = new();
    [ObservableProperty] private List<CategoryScore> _strongAreas = new();
    [ObservableProperty] private List<DomainScore> _domainScores = new();
    [ObservableProperty] private string _weakestCategory = "";
    [ObservableProperty] private string _syncStatus = "";
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private string _errorMessage = "";

    // Custom SkiaSharp chart data
    [ObservableProperty] private List<CategoryScore> _topCategories = new();
    [ObservableProperty] private bool _hasRadarData;

    public ResultsViewModel(SessionDbService db, S3SyncService s3, AuthService auth)
    {
        _db = db; _s3 = s3; _auth = auth;
    }

    public async Task LoadResultsAsync()
    {
        if (!int.TryParse(SessionIdParam, out var id))
        {
            Console.WriteLine($"[Results] Invalid SessionIdParam: '{SessionIdParam}'");
            ErrorMessage = "Invalid session ID.";
            return;
        }

        IsBusy = true;
        try
        {
            Console.WriteLine($"[Results] Loading session {id}");
            var session = await _db.GetSessionByIdAsync(id);
            if (session is null)
            {
                Console.WriteLine($"[Results] Session {id} not found in DB");
                ErrorMessage = "Session not found.";
                return;
            }

            Console.WriteLine($"[Results] Building result for session {id} ({session.TotalQuestions}Q)");
            _result = await _db.BuildResultAsync(session);

            // Metric cards
            ScoreText   = $"{_result.ScorePercent}%";
            CorrectText = $"{_result.Correct}/{_result.Total}";
            TimeText    = _result.Duration.TotalMinutes < 1
                ? $"{(int)_result.Duration.TotalSeconds}s"
                : $"{(int)_result.Duration.TotalMinutes}m {_result.Duration.Seconds}s";
            AvgTimeText = $"{_result.AvgSecondsPerQuestion:F1}s";

            DomainScores = _result.DomainScores ?? new();
            NeedsWork    = (_result.CategoryScores ?? new()).Where(c => c.Percent < 65).OrderBy(c => c.Percent).ToList();
            StrongAreas  = (_result.CategoryScores ?? new()).Where(c => c.Percent > 80).OrderByDescending(c => c.Percent).ToList();
            WeakestCategory = _result.WeakestCategory ?? "";

            // Radar chart data — top 8 categories by question count
            if (_result.CategoryScores is { Count: >= 3 })
            {
                TopCategories = _result.CategoryScores
                    .OrderByDescending(c => c.Total)
                    .Take(8)
                    .OrderBy(c => c.Category)
                    .ToList();
                HasRadarData = true;
            }

            HasResults = true;
            Console.WriteLine($"[Results] Load complete — {_result.ScorePercent}%");

            _ = SyncToS3Async();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Results] EXCEPTION in LoadResultsAsync: {ex}");
            App.LogCrash("ResultsViewModel.LoadResultsAsync", ex);
            ErrorMessage = $"Error loading results: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private async Task SyncToS3Async()
    {
        try
        {
            var user = await _auth.GetUserInfoAsync();
            if (user is null) return;

            var allSessions = await _db.GetAllSessionsAsync(user.Sub);
            await _s3.UploadSessionsAsync(user.Sub, allSessions);
            MainThread.BeginInvokeOnMainThread(() => SyncStatus = _s3.SyncStatus);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Results] S3 sync error (non-fatal): {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() => SyncStatus = $"Sync failed: {ex.Message}");
        }
    }

    // ── Action buttons ───────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RetryWrongAsync()
    {
        if (_result?.WrongQuestionIds.Count is null or 0) return;
        var ids = string.Join(",", _result.WrongQuestionIds);
        await Shell.Current.GoToAsync($"quiz?mode=RetryWrong&ids={Uri.EscapeDataString(ids)}");
    }

    [RelayCommand]
    private async Task DrillWeakestAsync()
    {
        if (string.IsNullOrEmpty(WeakestCategory)) return;
        await Shell.Current.GoToAsync(
            $"quiz?mode=DrillCategory&filter={Uri.EscapeDataString(WeakestCategory)}");
    }

    [RelayCommand]
    private async Task GoHomeAsync() =>
        await Shell.Current.GoToAsync("//dashboard");
}

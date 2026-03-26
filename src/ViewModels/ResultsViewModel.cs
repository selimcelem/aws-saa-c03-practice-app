using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
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

    // LiveCharts
    [ObservableProperty] private ISeries[] _domainBarSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _domainBarYAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _domainBarXAxes = Array.Empty<Axis>();
    [ObservableProperty] private ISeries[] _radarSeries = Array.Empty<ISeries>();
    [ObservableProperty] private PolarAxis[] _radarAngleAxes = Array.Empty<PolarAxis>();

    public ResultsViewModel(SessionDbService db, S3SyncService s3, AuthService auth)
    {
        _db = db; _s3 = s3; _auth = auth;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!int.TryParse(SessionIdParam, out var id)) return;
        IsBusy = true;
        try
        {
            var session = await _db.GetSessionByIdAsync(id);
            if (session is null) return;

            _result = await _db.BuildResultAsync(session);

            // Metric cards
            ScoreText   = $"{_result.ScorePercent}%";
            CorrectText = $"{_result.Correct} / {_result.Total}";
            TimeText    = _result.Duration.TotalMinutes < 1
                ? $"{(int)_result.Duration.TotalSeconds}s"
                : $"{(int)_result.Duration.TotalMinutes}m {_result.Duration.Seconds}s";
            AvgTimeText = $"{_result.AvgSecondsPerQuestion:F1}s / Q";

            DomainScores = _result.DomainScores;
            NeedsWork    = _result.CategoryScores.Where(c => c.Percent < 65).OrderBy(c => c.Percent).ToList();
            StrongAreas  = _result.CategoryScores.Where(c => c.Percent > 80).OrderByDescending(c => c.Percent).ToList();
            WeakestCategory = _result.WeakestCategory ?? "";

            BuildBarChart(_result.DomainScores);
            BuildRadarChart(_result.CategoryScores);

            // Sync result to S3
            var user = await _auth.GetUserInfoAsync();
            if (user is not null)
            {
                var allSessions = await _db.GetAllSessionsAsync(user.Sub);
                _ = Task.Run(async () =>
                {
                    await _s3.UploadSessionsAsync(user.Sub, allSessions);
                    SyncStatus = _s3.SyncStatus;
                });
            }
        }
        finally { IsBusy = false; }
    }

    private void BuildBarChart(List<DomainScore> domains)
    {
        // Abbreviated domain labels
        var labels = domains.Select(d => d.Domain.Replace("Design ", "").Replace(" Architectures", "")).ToArray();

        DomainBarSeries = new ISeries[]
        {
            new RowSeries<double>
            {
                Values      = domains.Select(d => d.Percent).ToArray(),
                Fill        = new SolidColorPaint(SKColor.Parse("#58a6ff")),
                Stroke      = null,
                MaxBarWidth = 30,
                DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#c9d1d9")),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                DataLabelsFormatter = p => $"{p.Model:F0}%",
                DataLabelsPadding = new LiveChartsCore.Drawing.Padding(4),
            }
        };

        DomainBarYAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#c9d1d9")),
                TicksPaint  = null,
                SeparatorsPaint = null,
            }
        };

        DomainBarXAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0, MaxLimit = 100,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#8b949e")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#30363d")),
                TicksPaint = null,
                Labeler = v => $"{v:F0}%",
            }
        };
    }

    private void BuildRadarChart(List<CategoryScore> categories)
    {
        // Limit to top N categories by total attempts for readability
        var top = categories.OrderByDescending(c => c.Total).Take(10).OrderBy(c => c.Category).ToList();

        RadarSeries = new ISeries[]
        {
            new PolarLineSeries<double>
            {
                Values = top.Select(c => c.Percent).ToArray(),
                IsClosed = true,
                Fill     = new SolidColorPaint(SKColor.Parse("#58a6ff").WithAlpha(40)),
                Stroke   = new SolidColorPaint(SKColor.Parse("#58a6ff"), 2),
                GeometrySize = 6,
                GeometryFill = new SolidColorPaint(SKColor.Parse("#58a6ff")),
                DataLabelsPaint = null,
            }
        };

        RadarAngleAxes = new PolarAxis[]
        {
            new PolarAxis
            {
                Labels = top.Select(c => c.Category).ToArray(),
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#c9d1d9")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#30363d")),
            }
        };
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

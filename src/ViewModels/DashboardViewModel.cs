using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AwsSaaC03Practice.Models;
using AwsSaaC03Practice.Services;

namespace AwsSaaC03Practice.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly AuthService _auth;
    private readonly SessionDbService _db;
    private readonly S3SyncService _s3;

    [ObservableProperty] private string _userName = "Student";
    [ObservableProperty] private string _userEmail = "";
    [ObservableProperty] private string _userSub = "";
    [ObservableProperty] private double _overallScore;
    [ObservableProperty] private int _totalSessions;
    [ObservableProperty] private int _studyStreak;
    [ObservableProperty] private string _lastSessionSummary = "No sessions yet";
    [ObservableProperty] private string _syncStatus = "";
    [ObservableProperty] private List<DomainScore> _domainScores = new();

    public DashboardViewModel(AuthService auth, SessionDbService db, S3SyncService s3)
    {
        _auth = auth; _db = db; _s3 = s3;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var user = await _auth.GetUserInfoAsync();
            if (user is not null)
            {
                UserName = user.Name.Split(' ')[0];
                UserEmail = user.Email;
                UserSub = user.Sub;
            }

            var sessions = await _db.GetAllSessionsAsync(UserSub);
            TotalSessions = sessions.Count;
            StudyStreak = await _db.GetStreakAsync(UserSub);

            if (sessions.Count > 0)
            {
                var allAnswers = sessions
                    .SelectMany(s => System.Text.Json.JsonSerializer
                        .Deserialize<List<AnswerRecord>>(s.AnswerDataJson)
                        ?? new List<AnswerRecord>())
                    .ToList();

                if (allAnswers.Count > 0)
                {
                    OverallScore = Math.Round(
                        (double)allAnswers.Count(a => a.IsCorrect) / allAnswers.Count * 100, 1);

                    DomainScores = allAnswers
                        .GroupBy(a => a.Domain)
                        .Select(g => new DomainScore
                        {
                            Domain  = g.Key,
                            Correct = g.Count(a => a.IsCorrect),
                            Total   = g.Count(),
                        })
                        .OrderBy(d => d.Domain)
                        .ToList();
                }

                var last = sessions.First();
                LastSessionSummary = $"{last.Mode} — {last.ScorePercent}% " +
                                     $"({last.CorrectAnswers}/{last.TotalQuestions}) " +
                                     $"on {last.StartedAt:MMM d}";
            }

            // Background S3 sync
            _ = Task.Run(async () =>
            {
                await _s3.UploadSessionsAsync(UserSub, sessions);
                SyncStatus = _s3.SyncStatus;
            });
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _auth.SignOutAsync();
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    private async Task GoToModePicker() =>
        await Shell.Current.GoToAsync("//modepicker");
}

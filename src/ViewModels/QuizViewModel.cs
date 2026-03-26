using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AwsSaaC03Practice.Models;
using AwsSaaC03Practice.Services;

namespace AwsSaaC03Practice.ViewModels;

[QueryProperty(nameof(ModeParam), "mode")]
[QueryProperty(nameof(FilterParam), "filter")]   // optional category filter
[QueryProperty(nameof(IdsParam), "ids")]          // optional comma-separated question IDs
public partial class QuizViewModel : BaseViewModel, IDisposable
{
    private readonly QuestionService _questions;
    private readonly SessionDbService _db;
    private readonly AuthService _auth;

    private List<Question> _deck = new();
    private List<AnswerRecord> _answers = new();
    private DateTime _questionStart;
    private IDispatcherTimer? _timer;
    private QuizSession? _currentSession;

    // ── Navigation params ───────────────────────────────────────────────────
    [ObservableProperty] private string _modeParam = "Random";
    [ObservableProperty] private string _filterParam = "";
    [ObservableProperty] private string _idsParam = "";

    // ── Observables ─────────────────────────────────────────────────────────
    [ObservableProperty] private Question? _currentQuestion;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int? _selectedOptionIndex;     // null = unanswered
    [ObservableProperty] private bool _answerRevealed;
    [ObservableProperty] private string _timerText = "";
    [ObservableProperty] private bool _timerVisible;
    [ObservableProperty] private string _progressText = "";

    // Per-option state after reveal
    [ObservableProperty] private bool _opt0Correct; [ObservableProperty] private bool _opt0Wrong;
    [ObservableProperty] private bool _opt1Correct; [ObservableProperty] private bool _opt1Wrong;
    [ObservableProperty] private bool _opt2Correct; [ObservableProperty] private bool _opt2Wrong;
    [ObservableProperty] private bool _opt3Correct; [ObservableProperty] private bool _opt3Wrong;

    private QuizMode _mode;
    private TimeSpan _remaining;

    public QuizViewModel(QuestionService questions, SessionDbService db, AuthService auth)
    {
        _questions = questions; _db = db; _auth = auth;
    }

    public async Task InitialiseAsync()
    {
        _mode = Enum.Parse<QuizMode>(ModeParam);
        var ids = string.IsNullOrEmpty(IdsParam)
            ? null : IdsParam.Split(',').ToList();

        _deck = _questions.GetForMode(_mode, FilterParam, ids);
        TotalCount = _deck.Count;

        // Start session record
        var user = await _auth.GetUserInfoAsync();
        _currentSession = new QuizSession
        {
            Mode      = _mode.ToString(),
            UserSub   = user?.Sub ?? "",
            StartedAt = DateTime.Now,
        };
        await _db.SaveSessionAsync(_currentSession);

        // Timer
        var limit = _mode.TimeLimit();
        if (limit.HasValue)
        {
            _remaining = limit.Value;
            TimerVisible = true;
            _timer = Application.Current!.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        ShowQuestion(0);
    }

    private void ShowQuestion(int index)
    {
        CurrentIndex = index;
        CurrentQuestion = _deck[index];
        SelectedOptionIndex = null;
        AnswerRevealed = false;
        ResetOptionColours();
        ProgressText = $"Question {index + 1} of {TotalCount}";
        _questionStart = DateTime.Now;
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectAnswer(string indexStr)
    {
        if (AnswerRevealed || CurrentQuestion is null) return;
        SelectedOptionIndex = int.Parse(indexStr);
        AnswerRevealed = true;

        var correct = CurrentQuestion.Correct;
        SetOptionColour(SelectedOptionIndex.Value, correct);

        _answers.Add(new AnswerRecord
        {
            QuestionId     = CurrentQuestion.Id,
            Domain         = CurrentQuestion.Domain,
            Category       = CurrentQuestion.Category,
            SelectedOption = SelectedOptionIndex.Value,
            CorrectOption  = correct,
            IsCorrect      = SelectedOptionIndex.Value == correct,
            SecondsSpent   = (DateTime.Now - _questionStart).TotalSeconds,
        });
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (!AnswerRevealed) return;
        var nextIndex = CurrentIndex + 1;
        if (nextIndex < _deck.Count)
            ShowQuestion(nextIndex);
        else
            await FinishSessionAsync(completed: true);
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "End session?",
            "End this session and see your results?",
            "End session", "Keep going");
        if (confirm)
            await FinishSessionAsync(completed: false);
    }

    private async Task FinishSessionAsync(bool completed)
    {
        _timer?.Stop();
        if (_currentSession is null) return;

        _currentSession.FinishedAt = DateTime.Now;
        _currentSession.TotalQuestions = _answers.Count;
        _currentSession.CorrectAnswers = _answers.Count(a => a.IsCorrect);
        _currentSession.Completed = completed;
        _currentSession.AnswerDataJson = JsonSerializer.Serialize(_answers);
        await _db.SaveSessionAsync(_currentSession);

        // Navigate to results
        var sessionId = _currentSession.Id;
        await Shell.Current.GoToAsync($"results?sessionId={sessionId}");
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _remaining -= TimeSpan.FromSeconds(1);
        var m = (int)_remaining.TotalMinutes;
        var s = _remaining.Seconds;
        TimerText = $"{m:D2}:{s:D2}";
        if (_remaining <= TimeSpan.Zero)
        {
            _timer?.Stop();
            MainThread.BeginInvokeOnMainThread(async () =>
                await FinishSessionAsync(completed: true));
        }
    }

    private void ResetOptionColours()
    {
        Opt0Correct = Opt0Wrong = Opt1Correct = Opt1Wrong =
        Opt2Correct = Opt2Wrong = Opt3Correct = Opt3Wrong = false;
    }

    private void SetOptionColour(int selected, int correct)
    {
        // Mark the correct answer green always
        switch (correct)
        {
            case 0: Opt0Correct = true; break;
            case 1: Opt1Correct = true; break;
            case 2: Opt2Correct = true; break;
            case 3: Opt3Correct = true; break;
        }
        // If selected != correct, mark selected red
        if (selected != correct)
        {
            switch (selected)
            {
                case 0: Opt0Wrong = true; break;
                case 1: Opt1Wrong = true; break;
                case 2: Opt2Wrong = true; break;
                case 3: Opt3Wrong = true; break;
            }
        }
    }

    public void Dispose() => _timer?.Stop();
}

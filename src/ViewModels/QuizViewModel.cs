using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AwsSaaC03Practice.Models;
using AwsSaaC03Practice.Services;

namespace AwsSaaC03Practice.ViewModels;

[QueryProperty(nameof(ModeParam), "mode")]
[QueryProperty(nameof(FilterParam), "filter")]
[QueryProperty(nameof(IdsParam), "ids")]
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
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int? _selectedOptionIndex;
    [ObservableProperty] private bool _answerRevealed;
    [ObservableProperty] private string _timerText = "";
    [ObservableProperty] private bool _timerVisible;
    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private string _explanationText = "";
    [ObservableProperty] private string _questionText = "";
    [ObservableProperty] private string _categoryText = "";

    private static readonly Color _colDefault = Color.FromArgb("#1c2128");
    private static readonly Color _colGreen   = Color.FromArgb("#0d2b18");
    private static readonly Color _colRed     = Color.FromArgb("#2d1a1a");

    [ObservableProperty] private Color _opt0Colour = Color.FromArgb("#1c2128");
    [ObservableProperty] private Color _opt1Colour = Color.FromArgb("#1c2128");
    [ObservableProperty] private Color _opt2Colour = Color.FromArgb("#1c2128");
    [ObservableProperty] private Color _opt3Colour = Color.FromArgb("#1c2128");

    [ObservableProperty] private string _option0Text = "";
    [ObservableProperty] private string _option1Text = "";
    [ObservableProperty] private string _option2Text = "";
    [ObservableProperty] private string _option3Text = "";

    private QuizMode _mode;
    private TimeSpan _remaining;

    public QuizViewModel(QuestionService questions, SessionDbService db, AuthService auth)
    {
        _questions = questions; _db = db; _auth = auth;
    }

    public async Task InitialiseAsync()
    {
        await _questions.EnsureLoadedAsync();
        _mode = Enum.Parse<QuizMode>(ModeParam);
        var ids = string.IsNullOrEmpty(IdsParam)
            ? null : IdsParam.Split(',').ToList();

        _deck = _questions.GetForMode(_mode, FilterParam, ids);
        TotalCount = _deck.Count;

        var user = await _auth.GetUserInfoAsync();
        _currentSession = new QuizSession
        {
            Mode      = _mode.ToString(),
            UserSub   = user?.Sub ?? "",
            StartedAt = DateTime.Now,
        };
        await _db.SaveSessionAsync(_currentSession);

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
        var q = _deck[index];
        CurrentIndex = index;

        QuestionText = q.Text;
        CategoryText = q.Category;
        Option0Text = q.Options[0];
        Option1Text = q.Options[1];
        Option2Text = q.Options[2];
        Option3Text = q.Options[3];
        ExplanationText = q.Explanation;

        SelectedOptionIndex = null;
        AnswerRevealed = false;
        ResetOptionColours();
        ProgressText = $"Question {index + 1} of {TotalCount}";
        _questionStart = DateTime.Now;
    }

    [RelayCommand]
    private void SelectAnswer(string indexStr)
    {
        if (AnswerRevealed) return;
        var q = _deck[CurrentIndex];
        SelectedOptionIndex = int.Parse(indexStr);
        AnswerRevealed = true;

        SetOptionColour(SelectedOptionIndex.Value, q.Correct);

        _answers.Add(new AnswerRecord
        {
            QuestionId     = q.Id,
            Domain         = q.Domain,
            Category       = q.Category,
            SelectedOption = SelectedOptionIndex.Value,
            CorrectOption  = q.Correct,
            IsCorrect      = SelectedOptionIndex.Value == q.Correct,
            SecondsSpent   = (DateTime.Now - _questionStart).TotalSeconds,
        });
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (!AnswerRevealed) return;
        if (CurrentIndex + 1 < _deck.Count)
            ShowQuestion(CurrentIndex + 1);
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

        try
        {
            _currentSession.FinishedAt = DateTime.Now;
            _currentSession.TotalQuestions = _answers.Count;
            _currentSession.CorrectAnswers = _answers.Count(a => a.IsCorrect);
            _currentSession.Completed = completed;
            _currentSession.AnswerDataJson = JsonSerializer.Serialize(_answers);
            await _db.SaveSessionAsync(_currentSession);

            await Shell.Current.GoToAsync($"results?sessionId={_currentSession.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Quiz] EXCEPTION in FinishSessionAsync: {ex}");
            App.LogCrash("QuizViewModel.FinishSessionAsync", ex);
            try
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to show results: {ex.Message}", "OK");
                await Shell.Current.GoToAsync("//dashboard");
            }
            catch { }
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _remaining -= TimeSpan.FromSeconds(1);
        TimerText = $"{(int)_remaining.TotalMinutes:D2}:{_remaining.Seconds:D2}";
        if (_remaining <= TimeSpan.Zero)
        {
            _timer?.Stop();
            MainThread.BeginInvokeOnMainThread(async () =>
                await FinishSessionAsync(completed: true));
        }
    }

    private void ResetOptionColours()
    {
        Opt0Colour = Opt1Colour = Opt2Colour = Opt3Colour = _colDefault;
    }

    private void SetOptionColour(int selected, int correct)
    {
        ResetOptionColours();
        SetColour(correct, _colGreen);
        if (selected != correct)
            SetColour(selected, _colRed);
    }

    private void SetColour(int index, Color colour)
    {
        switch (index)
        {
            case 0: Opt0Colour = colour; break;
            case 1: Opt1Colour = colour; break;
            case 2: Opt2Colour = colour; break;
            case 3: Opt3Colour = colour; break;
        }
    }

    public void Dispose() => _timer?.Stop();
}

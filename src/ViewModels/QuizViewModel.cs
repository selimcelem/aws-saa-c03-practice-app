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
    private int[] _shuffleMap = new int[4]; // maps display index → original option index
    private int _shuffledCorrectIndex;

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
    [ObservableProperty] private string _explanationText = "";

    // Per-option button background colour after answer is revealed
    private static readonly Color _colDefault = Color.FromArgb("#1c2128");
    private static readonly Color _colGreen   = Color.FromArgb("#0d2b18");
    private static readonly Color _colRed     = Color.FromArgb("#2d1a1a");

    [ObservableProperty] private Color _opt0Colour = Color.FromArgb("#1c2128");
    [ObservableProperty] private Color _opt1Colour = Color.FromArgb("#1c2128");
    [ObservableProperty] private Color _opt2Colour = Color.FromArgb("#1c2128");
    [ObservableProperty] private Color _opt3Colour = Color.FromArgb("#1c2128");

    // Shuffled option texts — display order differs from JSON order each time
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
        var q = _deck[index];
        CurrentIndex = index;
        CurrentQuestion = q;

        // Shuffle option CONTENT into fixed A/B/C/D label positions.
        // _shuffleMap[displayPos] = originalIndex — tells us which original option sits at each position.
        _shuffleMap = Enumerable.Range(0, q.Options.Count)
            .OrderBy(_ => Random.Shared.Next()).ToArray();
        _shuffledCorrectIndex = Array.IndexOf(_shuffleMap, q.Correct);

        string[] labels = ["A", "B", "C", "D"];
        Option0Text = labels[0] + ". " + StripPrefix(q.Options[_shuffleMap[0]]);
        Option1Text = labels[1] + ". " + StripPrefix(q.Options[_shuffleMap[1]]);
        Option2Text = labels[2] + ". " + StripPrefix(q.Options[_shuffleMap[2]]);
        Option3Text = labels[3] + ". " + StripPrefix(q.Options[_shuffleMap[3]]);

        // Explanations describe approaches by name (not by letter), so no remapping needed
        ExplanationText = q.Explanation;

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

        // Highlight using shuffled positions
        SetOptionColour(SelectedOptionIndex.Value, _shuffledCorrectIndex);

        // Map display indices back to original JSON indices for recording
        var originalSelected = _shuffleMap[SelectedOptionIndex.Value];
        var originalCorrect  = CurrentQuestion.Correct;

        _answers.Add(new AnswerRecord
        {
            QuestionId     = CurrentQuestion.Id,
            Domain         = CurrentQuestion.Domain,
            Category       = CurrentQuestion.Category,
            SelectedOption = originalSelected,
            CorrectOption  = originalCorrect,
            IsCorrect      = originalSelected == originalCorrect,
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

        try
        {
            Console.WriteLine($"[Quiz] Finishing session (completed={completed}, answers={_answers.Count})");

            _currentSession.FinishedAt = DateTime.Now;
            _currentSession.TotalQuestions = _answers.Count;
            _currentSession.CorrectAnswers = _answers.Count(a => a.IsCorrect);
            _currentSession.Completed = completed;
            _currentSession.AnswerDataJson = JsonSerializer.Serialize(_answers);
            await _db.SaveSessionAsync(_currentSession);

            var sessionId = _currentSession.Id;
            Console.WriteLine($"[Quiz] Session saved (id={sessionId}), navigating to results");
            await Shell.Current.GoToAsync($"results?sessionId={sessionId}");
            Console.WriteLine($"[Quiz] Navigation to results complete");
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
            catch (Exception navEx)
            {
                Console.WriteLine($"[Quiz] EXCEPTION in error recovery: {navEx}");
            }
        }
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
        Opt0Colour = Opt1Colour = Opt2Colour = Opt3Colour = _colDefault;
    }

    private void SetOptionColour(int selected, int correct)
    {
        ResetOptionColours();
        // Always highlight the correct answer green
        SetColour(correct, _colGreen);
        // If the user was wrong, highlight their pick red
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

    private static string StripPrefix(string option)
    {
        if (option.Length >= 3 && option[1] == '.' && option[2] == ' ' && option[0] is >= 'A' and <= 'D')
            return option[3..];
        return option;
    }

    public void Dispose() => _timer?.Stop();
}

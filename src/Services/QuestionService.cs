using System.Text.Json;
using AwsSaaC03Practice.Models;

namespace AwsSaaC03Practice.Services;

public class QuestionService
{
    private List<Question>? _allQuestions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task EnsureLoadedAsync()
    {
        if (_allQuestions is not null) return;
        await _lock.WaitAsync();
        try
        {
            if (_allQuestions is not null) return;
            using var stream = await FileSystem.OpenAppPackageFileAsync("questions.json");
            _allQuestions = await JsonSerializer.DeserializeAsync<List<Question>>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("Failed to parse questions.json");

            // Warn about questions where correct answer is significantly longer than distractors
            foreach (var q in _allQuestions)
            {
                if (q.Options.Count != 4 || q.Correct < 0 || q.Correct >= q.Options.Count) continue;
                var correctLen = q.Options[q.Correct].Length;
                var otherLens = q.Options.Where((_, i) => i != q.Correct).Select(o => o.Length).ToList();
                var avgOther = otherLens.Average();
                if (avgOther > 0 && correctLen > avgOther * 1.4)
                    Console.WriteLine($"[Questions] Length bias in {q.Id}: correct={correctLen}ch, avg_other={avgOther:F0}ch ({correctLen / avgOther:F1}x)");
            }
        }
        finally { _lock.Release(); }
    }

    // Keep for backward compat
    public Task LoadAsync() => EnsureLoadedAsync();

    public IReadOnlyList<Question> All =>
        _allQuestions ?? throw new InvalidOperationException("Questions not loaded. Call EnsureLoadedAsync() first.");

    public List<Question> GetForMode(QuizMode mode, string? filterCategory = null,
        List<string>? specificIds = null)
    {
        IEnumerable<Question> pool = All;

        if (specificIds?.Count > 0)
        {
            var idSet = specificIds.ToHashSet();
            pool = All.Where(q => idSet.Contains(q.Id));
        }
        else if (!string.IsNullOrEmpty(filterCategory))
        {
            pool = All.Where(q => q.Category == filterCategory);
        }

        var shuffled = pool.OrderBy(_ => Random.Shared.Next()).ToList();
        var count = mode.QuestionCount();
        return count.HasValue ? shuffled.Take(count.Value).ToList() : shuffled;
    }

    public List<string> GetAllCategories() =>
        _allQuestions?.Select(q => q.Category).Distinct().OrderBy(c => c).ToList() ?? new();

    public List<string> GetAllDomains() =>
        _allQuestions?.Select(q => q.Domain).Distinct().ToList() ?? new();
}

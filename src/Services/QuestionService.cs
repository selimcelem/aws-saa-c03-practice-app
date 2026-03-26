using System.Text.Json;
using AwsSaaC03Practice.Models;

namespace AwsSaaC03Practice.Services;

public class QuestionService
{
    private List<Question>? _allQuestions;

    public async Task LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("questions.json");
        _allQuestions = await JsonSerializer.DeserializeAsync<List<Question>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new Exception("Failed to parse questions.json");
    }

    public IReadOnlyList<Question> All =>
        _allQuestions ?? throw new InvalidOperationException("Questions not loaded.");

    public List<Question> GetForMode(QuizMode mode, string? filterCategory = null,
        List<string>? specificIds = null)
    {
        IEnumerable<Question> pool = All;

        // Override pool for special modes
        if (specificIds?.Count > 0)
        {
            var idSet = specificIds.ToHashSet();
            pool = All.Where(q => idSet.Contains(q.Id));
        }
        else if (!string.IsNullOrEmpty(filterCategory))
        {
            pool = All.Where(q => q.Category == filterCategory);
        }

        // Shuffle
        var shuffled = pool.OrderBy(_ => Random.Shared.Next()).ToList();

        // Trim to count
        var count = mode.QuestionCount();
        return count.HasValue ? shuffled.Take(count.Value).ToList() : shuffled;
    }

    public List<string> GetAllCategories() =>
        _allQuestions?.Select(q => q.Category).Distinct().OrderBy(c => c).ToList() ?? new();

    public List<string> GetAllDomains() =>
        _allQuestions?.Select(q => q.Domain).Distinct().ToList() ?? new();
}

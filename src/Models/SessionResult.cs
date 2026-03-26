namespace AwsSaaC03Practice.Models;

public class SessionResult
{
    public QuizMode Mode { get; set; }
    public int Correct { get; set; }
    public int Total { get; set; }
    public double ScorePercent => Total > 0 ? Math.Round((double)Correct / Total * 100, 1) : 0;
    public TimeSpan Duration { get; set; }
    public double AvgSecondsPerQuestion => Total > 0 ? Duration.TotalSeconds / Total : 0;

    public List<DomainScore> DomainScores { get; set; } = new();
    public List<CategoryScore> CategoryScores { get; set; } = new();

    // Questions the user got wrong — used for "Retry wrong answers"
    public List<string> WrongQuestionIds { get; set; } = new();

    // Weakest category for "Drill" button — only if below 65%
    public string? WeakestCategory =>
        CategoryScores.Where(c => c.Total >= 2 && c.Percent < 65)
                      .OrderBy(c => c.Percent)
                      .FirstOrDefault()?.Category;
}

public class DomainScore
{
    public string Domain { get; set; } = "";
    public int Correct { get; set; }
    public int Total { get; set; }
    public double Percent => Total > 0 ? Math.Round((double)Correct / Total * 100, 1) : 0;
    public double Fraction => Percent / 100.0;

    // Colour thresholds: ≥75 green, 60–74 amber, <60 red
    public string Colour => Percent >= 75 ? "#3fb950" : Percent >= 60 ? "#d29922" : "#f85149";
}

public class CategoryScore
{
    public string Category { get; set; } = "";
    public int Correct { get; set; }
    public int Total { get; set; }
    public double Percent => Total > 0 ? Math.Round((double)Correct / Total * 100, 1) : 0;
    public string Display => $"{Category} ({Percent:F0}%)";
}

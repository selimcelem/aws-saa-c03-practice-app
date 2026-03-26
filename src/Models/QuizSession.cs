using SQLite;

namespace AwsSaaC03Practice.Models;

[Table("sessions")]
public class QuizSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Mode { get; set; } = "";
    public string UserSub { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public bool Completed { get; set; }   // false = user stopped early
    public string AnswerDataJson { get; set; } = "[]"; // serialized List<AnswerRecord>

    [Ignore]
    public double ScorePercent =>
        TotalQuestions > 0 ? Math.Round((double)CorrectAnswers / TotalQuestions * 100, 1) : 0;

    [Ignore]
    public TimeSpan Duration =>
        FinishedAt.HasValue ? FinishedAt.Value - StartedAt : TimeSpan.Zero;
}

/// <summary>Per-question answer stored inside QuizSession.AnswerDataJson.</summary>
public class AnswerRecord
{
    public string QuestionId { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Category { get; set; } = "";
    public int SelectedOption { get; set; }
    public int CorrectOption { get; set; }
    public bool IsCorrect { get; set; }
    public double SecondsSpent { get; set; }
}

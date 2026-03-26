namespace AwsSaaC03Practice.Models;

public enum QuizMode
{
    Random,        // User stops anytime; no timer
    ExamSim,       // 65 questions, 130-minute countdown
    Quick30,       // 30 questions, no timer
    Quick10,       // 10 questions, no timer
    RetryWrong,    // Wrong answers from previous session
    DrillCategory  // Single category drill
}

public static class QuizModeExtensions
{
    public static int? QuestionCount(this QuizMode mode) => mode switch
    {
        QuizMode.ExamSim      => 65,
        QuizMode.Quick30      => 30,
        QuizMode.Quick10      => 10,
        _                     => null   // null = all available / user stops
    };

    public static TimeSpan? TimeLimit(this QuizMode mode) => mode switch
    {
        QuizMode.ExamSim => TimeSpan.FromMinutes(130),
        _                => null
    };

    public static string DisplayName(this QuizMode mode) => mode switch
    {
        QuizMode.Random       => "Random",
        QuizMode.ExamSim      => "Exam Simulation",
        QuizMode.Quick30      => "Quick 30",
        QuizMode.Quick10      => "Quick 10",
        QuizMode.RetryWrong   => "Retry Wrong Answers",
        QuizMode.DrillCategory => "Category Drill",
        _                     => mode.ToString()
    };
}

using System.Text.Json;
using AwsSaaC03Practice.Models;
using SQLite;

namespace AwsSaaC03Practice.Services;

public class SessionDbService
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetDb()
    {
        if (_db is not null) return _db;
        var path = Path.Combine(FileSystem.AppDataDirectory, "sessions.db");
        _db = new SQLiteAsyncConnection(path);
        await _db.CreateTableAsync<QuizSession>();
        await _db.CreateTableAsync<ReportedQuestion>();
        return _db;
    }

    public async Task<int> SaveSessionAsync(QuizSession session)
    {
        var db = await GetDb();
        if (session.Id == 0)
            await db.InsertAsync(session);
        else
            await db.UpdateAsync(session);
        return session.Id;
    }

    public async Task<List<QuizSession>> GetAllSessionsAsync(string userSub)
    {
        var db = await GetDb();
        return await db.Table<QuizSession>()
            .Where(s => s.UserSub == userSub)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
    }

    public async Task<SessionResult> BuildResultAsync(QuizSession session)
    {
        var json = session.AnswerDataJson;
        var answers = string.IsNullOrEmpty(json)
            ? new List<AnswerRecord>()
            : JsonSerializer.Deserialize<List<AnswerRecord>>(json) ?? new List<AnswerRecord>();

        var result = new SessionResult
        {
            Mode     = Enum.Parse<QuizMode>(session.Mode),
            Correct  = session.CorrectAnswers,
            Total    = session.TotalQuestions,
            Duration = session.Duration,
        };

        result.DomainScores = answers
            .GroupBy(a => a.Domain)
            .Select(g => new DomainScore
            {
                Domain  = g.Key,
                Correct = g.Count(a => a.IsCorrect),
                Total   = g.Count(),
            })
            .OrderBy(d => d.Domain)
            .ToList();

        result.CategoryScores = answers
            .GroupBy(a => a.Category)
            .Select(g => new CategoryScore
            {
                Category = g.Key,
                Correct  = g.Count(a => a.IsCorrect),
                Total    = g.Count(),
            })
            .OrderBy(c => c.Category)
            .ToList();

        result.WrongQuestionIds = answers
            .Where(a => !a.IsCorrect)
            .Select(a => a.QuestionId)
            .ToList();

        return result;
    }

    // Study streak: consecutive calendar days with at least one completed session
    public async Task<int> GetStreakAsync(string userSub)
    {
        var sessions = await GetAllSessionsAsync(userSub);
        if (sessions.Count == 0) return 0;

        var days = sessions
            .Select(s => s.StartedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int streak = 0;
        var expected = DateTime.Today;
        foreach (var day in days)
        {
            if (day == expected || day == expected.AddDays(-1))
            {
                streak++;
                expected = day.AddDays(-1);
            }
            else break;
        }
        return streak;
    }

    public async Task<QuizSession?> GetSessionByIdAsync(int id)
    {
        var db = await GetDb();
        return await db.Table<QuizSession>()
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<QuizSession?> GetLastSessionAsync(string userSub)
    {
        var db = await GetDb();
        return await db.Table<QuizSession>()
            .Where(s => s.UserSub == userSub)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task DeleteAllSessionsAsync(string userSub)
    {
        var db = await GetDb();
        await db.ExecuteAsync("DELETE FROM sessions WHERE UserSub = ?", userSub);
    }

    public async Task<bool> IsReportedThisSessionAsync(string questionId, DateTime sessionStart)
    {
        var db = await GetDb();
        var count = await db.Table<ReportedQuestion>()
            .Where(r => r.QuestionId == questionId && r.SessionStarted == sessionStart)
            .CountAsync();
        return count > 0;
    }

    public async Task MarkAsReportedAsync(string questionId, DateTime sessionStart)
    {
        var db = await GetDb();
        await db.InsertAsync(new ReportedQuestion
        {
            QuestionId = questionId,
            SessionStarted = sessionStart,
        });
    }
}

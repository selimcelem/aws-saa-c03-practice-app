using SQLite;

namespace AwsSaaC03Practice.Models;

[Table("reported_questions")]
public class ReportedQuestion
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string QuestionId { get; set; } = "";
    public DateTime SessionStarted { get; set; }
}

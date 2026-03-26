using System.Text.Json.Serialization;

namespace AwsSaaC03Practice.Models;

public class Question
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("question")]
    public string Text { get; set; } = "";

    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = new();

    [JsonPropertyName("correct")]
    public int Correct { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = "";
}

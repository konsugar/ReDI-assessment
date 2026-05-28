namespace ReDI.Assessment.Api.Models;

public class Assessment
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? TimeLimitMinutes { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<Question> Questions { get; set; } = new();

    public List<AssessmentTopic> Topics { get; set; } = new();
}
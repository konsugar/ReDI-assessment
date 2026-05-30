using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.Models;

public class Assessment
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int? TimeLimitMinutes { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<Question> Questions { get; set; } = new();

    public List<AssessmentTopic> Topics { get; set; } = new();

    public List<AssessmentAttempt> Attempts { get; set; } = new();
}
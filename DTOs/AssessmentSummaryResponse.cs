namespace ReDI.Assessment.Api.DTOs;

public class AssessmentSummaryResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? TimeLimitMinutes { get; set; }

    public DateTime CreatedUtc { get; set; }

    public int QuestionCount { get; set; }
}
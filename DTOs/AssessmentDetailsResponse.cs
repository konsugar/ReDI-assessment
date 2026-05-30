namespace ReDI.Assessment.Api.DTOs;

public class AssessmentDetailsResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? TimeLimitMinutes { get; set; }

    public DateTime CreatedUtc { get; set; }

    public List<QuestionResponse> Questions { get; set; } = new();

    public bool IsPublished { get; set; }
}

public class QuestionResponse
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public List<AnswerOptionResponse> AnswerOptions { get; set; } = new();
}

public class AnswerOptionResponse
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int SortOrder { get; set; }
}
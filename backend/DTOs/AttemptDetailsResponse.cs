namespace ReDI.Assessment.Api.DTOs;

public class AttemptDetailsResponse
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }

    public DateTime StartedUtc { get; set; }

    public DateTime? EndsUtc { get; set; }

    public DateTime CurrentUtc { get; set; }

    public int? RemainingSeconds { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? SubmittedUtc { get; set; }

    public bool IsSubmitted { get; set; }

    public bool IsExpired { get; set; }

    public List<AttemptQuestionResponse> Questions { get; set; } = new();
}

public class AttemptQuestionResponse
{
    public int AttemptQuestionId { get; set; }

    public int QuestionId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string? AnswerText { get; set; }

    public List<int> SelectedAnswerOptionIds { get; set; } = new();

    public List<AttemptAnswerOptionResponse> AnswerOptions { get; set; } = new();
}

public class AttemptAnswerOptionResponse
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
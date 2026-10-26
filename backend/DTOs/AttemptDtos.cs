namespace ReDI.Assessment.Api.DTOs;

public class AssessmentAttemptResponse
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

    public int QuestionCount { get; set; }
}

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

public class AttemptResultResponse
{
    public int AttemptId { get; set; }

    public int AssessmentId { get; set; }

    public DateTime StartedUtc { get; set; }

    public DateTime? EndsUtc { get; set; }

    public DateTime CurrentUtc { get; set; }

    public int? RemainingSeconds { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? SubmittedUtc { get; set; }

    public bool IsSubmitted { get; set; }

    public bool IsExpired { get; set; }

    public int QuestionCount { get; set; }

    public int AutomaticallyCheckedQuestionCount { get; set; }

    public int CorrectQuestionCount { get; set; }

    public List<AttemptQuestionResultResponse> Questions { get; set; } = new();
}

public class AttemptQuestionResultResponse
{
    public int AttemptQuestionId { get; set; }

    public int QuestionId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string? AnswerText { get; set; }

    public List<int> SelectedAnswerOptionIds { get; set; } = new();

    public bool? IsCorrect { get; set; }
}

public class AttemptStatusResponse
{
    public int AttemptId { get; set; }

    public int AssessmentId { get; set; }

    public DateTime StartedUtc { get; set; }

    public DateTime? EndsUtc { get; set; }

    public DateTime CurrentUtc { get; set; }

    public int? RemainingSeconds { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? SubmittedUtc { get; set; }

    public bool IsSubmitted { get; set; }

    public bool IsExpired { get; set; }
}

public class SubmitAttemptQuestionAnswerRequest
{
    public string? AnswerText { get; set; }

    public List<int> SelectedAnswerOptionIds { get; set; } = new();
}

public class SubmitAttemptResponse
{
    public int AttemptId { get; set; }

    public int AssessmentId { get; set; }

    public DateTime? EndsUtc { get; set; }

    public DateTime CurrentUtc { get; set; }

    public int? RemainingSeconds { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime SubmittedUtc { get; set; }

    public bool IsSubmitted { get; set; }

    public bool IsExpired { get; set; }

    public int QuestionCount { get; set; }

    public int AutomaticallyCheckedQuestionCount { get; set; }

    public int CorrectQuestionCount { get; set; }
}
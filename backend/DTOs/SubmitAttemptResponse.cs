namespace ReDI.Assessment.Api.DTOs;

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
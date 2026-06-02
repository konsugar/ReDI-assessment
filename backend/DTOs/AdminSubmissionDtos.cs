using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class AdminSubmissionSummaryResponse
{
    public int AttemptId { get; set; }

    public int AssessmentId { get; set; }

    public string SubmissionId { get; set; } = string.Empty;

    public string ParticipantCode { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public DateTime SubmittedUtc { get; set; }

    public int Score { get; set; }

    public int Total { get; set; }

    public int Percentage { get; set; }

    public List<FrontendSelfEvaluationResponse> SelfEvaluation { get; set; } = new();
}

public class AdminSubmissionDetailsResponse
{
    public int AttemptId { get; set; }

    public int AssessmentId { get; set; }

    public string SubmissionId { get; set; } = string.Empty;

    public string ParticipantCode { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public DateTime SubmittedUtc { get; set; }

    public int Score { get; set; }

    public int Total { get; set; }

    public int Percentage { get; set; }

    public List<FrontendQuestionResultResponse> QuestionResults { get; set; } = new();

    public List<FrontendSelfEvaluationResponse> SelfEvaluation { get; set; } = new();

    public List<FrontendOpenAnswerResponse> OpenAnswers { get; set; } = new();
}

public class EvaluateAttemptQuestionRequest
{
    public bool? IsCorrect { get; set; }
}

public class UpdateTeacherNoteRequest
{
    [MaxLength(2000)]
    public string TeacherNote { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class FrontendSubmissionRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string ParticipantCode { get; set; } = string.Empty;

    [Required]
    public List<FrontendSelfEvaluationRequest> SelfEvaluation { get; set; } = new();

    [Required]
    public List<FrontendMultipleChoiceAnswerRequest> MultipleChoiceAnswers { get; set; } = new();

    [Required]
    public List<FrontendOpenAnswerRequest> OpenAnswers { get; set; } = new();
}

public class FrontendSelfEvaluationRequest
{
    [Range(1, int.MaxValue)]
    public int TopicId { get; set; }

    // Sent by the current frontend for local display compatibility.
    // The backend does not trust this value and uses Topic.Name from the database.
    public string TopicName { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }
}

public class FrontendMultipleChoiceAnswerRequest
{
    [Range(1, int.MaxValue)]
    public int QuestionId { get; set; }

    [Range(0, int.MaxValue)]
    public int? SelectedOption { get; set; }
}

public class FrontendOpenAnswerRequest
{
    public int QuestionId { get; set; }

    // Sent by the current frontend for local display compatibility.
    // The backend does not trust this value and uses Question.Text from the database.
    public string QuestionText { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;
}

public class FrontendSubmissionResponse
{
    public string SubmissionId { get; set; } = string.Empty;

    public string ParticipantCode { get; set; } = string.Empty;

    public int Score { get; set; }

    public int Total { get; set; }

    public int Percentage { get; set; }

    public List<FrontendQuestionResultResponse> QuestionResults { get; set; } = new();

    public List<FrontendSelfEvaluationResponse> SelfEvaluation { get; set; } = new();

    public List<FrontendOpenAnswerResponse> OpenAnswers { get; set; } = new();
}

public class FrontendQuestionResultResponse
{
    public int QuestionId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public List<string> Options { get; set; } = new();

    public int? SelectedOption { get; set; }

    public int CorrectIndex { get; set; }

    public bool Correct { get; set; }
}

public class FrontendSelfEvaluationResponse
{
    public int TopicId { get; set; }

    public string TopicName { get; set; } = string.Empty;

    public int Rating { get; set; }
}

public class FrontendOpenAnswerResponse
{
    public int AttemptQuestionId { get; set; }

    public int QuestionId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public bool? IsCorrect { get; set; }

    public string EvaluationStatus { get; set; } = string.Empty;

    public string TeacherNote { get; set; } = string.Empty;
}
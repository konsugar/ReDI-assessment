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
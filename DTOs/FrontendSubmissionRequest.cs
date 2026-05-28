using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class FrontendSubmissionRequest
{
    [Required]
    [MinLength(1)]
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
    public int TopicId { get; set; }

    // Sent by the current frontend for local display compatibility.
    // The backend does not trust this value and uses Topic.Name from the database.
    public string TopicName { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }
}

public class FrontendMultipleChoiceAnswerRequest
{
    public int QuestionId { get; set; }

    public int? SelectedOption { get; set; }
}

public class FrontendOpenAnswerRequest
{
    public int QuestionId { get; set; }

    // Sent by the current frontend for local display compatibility.
    // The backend does not trust this value and uses Question.Text from the database.
    public string QuestionText { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
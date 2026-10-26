namespace ReDI.Assessment.Api.DTOs;

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
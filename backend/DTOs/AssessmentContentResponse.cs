using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.DTOs;

public class AssessmentContentResponse
{
    public AssessmentContentAssessmentResponse Assessment { get; set; } = new();

    public FrontendAssessmentSummaryResponse Status { get; set; } = new();

    public List<AssessmentContentTopicResponse> Topics { get; set; } = new();

    public List<AssessmentContentAvailableTopicResponse> AvailableTopics { get; set; } = new();

    public List<AssessmentContentQuestionTypeResponse> QuestionTypes { get; set; } = new();

    public List<AssessmentContentQuestionResponse> Questions { get; set; } = new();


}

public class AssessmentContentAssessmentResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? TimeLimitMinutes { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedUtc { get; set; }
}

public class AssessmentContentTopicResponse
{
    public int AssessmentTopicId { get; set; }

    public int TopicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public class AssessmentContentQuestionResponse
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; }

    public string QuestionTypeName { get; set; } = string.Empty;

    public bool IsSupportedForFrontend { get; set; }

    public int SortOrder { get; set; }

    public List<AssessmentContentAnswerOptionResponse> AnswerOptions { get; set; } = new();
}

public class AssessmentContentAnswerOptionResponse
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int SortOrder { get; set; }
}

public class AssessmentContentAvailableTopicResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class AssessmentContentQuestionTypeResponse
{
    public int Value { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsSupportedForFrontend { get; set; }

    public bool UsesAnswerOptions { get; set; }
}
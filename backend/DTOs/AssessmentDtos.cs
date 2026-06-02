using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class AssessmentSummaryResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? TimeLimitMinutes { get; set; }

    public DateTime CreatedUtc { get; set; }

    public int QuestionCount { get; set; }

    public bool IsPublished { get; set; }
}

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

public class CreateAssessmentRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int? TimeLimitMinutes { get; set; }
}

public class UpdateAssessmentRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 10000)]
    public int? TimeLimitMinutes { get; set; }
}

public class UpdateAssessmentPublicationRequest
{
    // Publication is changed only through the dedicated publication endpoint.
    public bool IsPublished { get; set; }
}


public class FrontendAssessmentSummaryResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int QuestionCount { get; set; }

    public int TopicCount { get; set; }

    public int SubmissionCount { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsPublished { get; set; }

    public bool IsReadyForFrontend { get; set; }

    public bool CanLaunchFrontend { get; set; }

    public List<string> ReadinessIssues { get; set; } = new();

    public bool CanEditStructure { get; set; }

    public string? StructureLockReason { get; set; }
}
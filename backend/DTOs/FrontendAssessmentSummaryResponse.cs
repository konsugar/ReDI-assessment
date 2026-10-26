namespace ReDI.Assessment.Api.DTOs;

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
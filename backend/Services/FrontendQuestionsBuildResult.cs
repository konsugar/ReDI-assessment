using ReDI.Assessment.Api.DTOs;

namespace ReDI.Assessment.Api.Services;

public enum FrontendQuestionsBuildStatus
{
    Ok = 1,
    NotFound = 2,
    NotPublished = 3,
    NotReady = 4
}

public class FrontendQuestionsBuildResult
{
    public FrontendQuestionsBuildStatus Status { get; set; }

    public FrontendQuestionsResponse? Response { get; set; }

    public string? Error { get; set; }

    public List<string> ReadinessIssues { get; set; } = new();
}
using ReDI.Assessment.Api.DTOs;

namespace ReDI.Assessment.Api.Services;

public enum FrontendSubmissionCreateStatus
{
    Created = 1,
    NotFound = 2,
    BadRequest = 3,
    NotReady = 4
}

public class FrontendSubmissionCreateResult
{
    public FrontendSubmissionCreateStatus Status { get; set; }

    public int? AttemptId { get; set; }

    public FrontendSubmissionResponse? Response { get; set; }

    public string? Error { get; set; }

    public List<string> ReadinessIssues { get; set; } = new();
}
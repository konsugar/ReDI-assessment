namespace ReDI.Assessment.Api.Services;

public class AttemptStatusInfo
{
    public DateTime CurrentUtc { get; set; }

    public int? RemainingSeconds { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsSubmitted { get; set; }

    public bool IsExpired { get; set; }
}
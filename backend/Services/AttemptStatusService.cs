using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.Services;

public class AttemptStatusService
{
    public AttemptStatusInfo GetStatus(AssessmentAttempt attempt)
    {
        var currentUtc = DateTime.UtcNow;

        var isSubmitted = attempt.SubmittedUtc != null;

        var isExpired = !isSubmitted &&
            attempt.EndsUtc != null &&
            currentUtc > attempt.EndsUtc;

        var status = isSubmitted
            ? "Submitted"
            : isExpired
                ? "Expired"
                : "InProgress";

        int? remainingSeconds = attempt.EndsUtc == null || isSubmitted
            ? null
            : Math.Max(0, (int)(attempt.EndsUtc.Value - currentUtc).TotalSeconds);

        return new AttemptStatusInfo
        {
            CurrentUtc = currentUtc,
            RemainingSeconds = remainingSeconds,
            Status = status,
            IsSubmitted = isSubmitted,
            IsExpired = isExpired
        };
    }
}
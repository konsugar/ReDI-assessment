using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.Models;

public class AttemptSelfEvaluation
{
    public int Id { get; set; }

    public int AssessmentAttemptId { get; set; }

    public AssessmentAttempt AssessmentAttempt { get; set; } = null!;

    public int TopicId { get; set; }

    [MaxLength(200)]
    public string TopicName { get; set; } = string.Empty;

    public int Rating { get; set; }
}
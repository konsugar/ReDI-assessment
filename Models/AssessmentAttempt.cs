namespace ReDI.Assessment.Api.Models;

public class AssessmentAttempt
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }

    public string ParticipantCode { get; set; } = string.Empty;

    public Assessment? Assessment { get; set; }

    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? EndsUtc { get; set; }

    public DateTime? SubmittedUtc { get; set; }

    public List<AttemptQuestion> AttemptQuestions { get; set; } = new();

    public List<AttemptSelfEvaluation> SelfEvaluations { get; set; } = new();
}
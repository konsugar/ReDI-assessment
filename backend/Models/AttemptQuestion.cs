using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.Models;
public class AttemptQuestion
{
    public int Id { get; set; }

    public int AssessmentAttemptId { get; set; }

    public AssessmentAttempt AssessmentAttempt { get; set; } = null!;

    public int QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public List<AttemptAnswerOption> SelectedAnswerOptions { get; set; } = new();

    [MaxLength(4000)]
    public string? AnswerText { get; set; }

    // Null means the answer has not been evaluated yet.
    // This is mainly used for free-text answers in the MVP.
    public bool? IsCorrect { get; set; }

    [MaxLength(2000)]
    public string TeacherNote { get; set; } = string.Empty;
}
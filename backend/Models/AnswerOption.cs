using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.Models;

public class AnswerOption
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int SortOrder { get; set; }
}
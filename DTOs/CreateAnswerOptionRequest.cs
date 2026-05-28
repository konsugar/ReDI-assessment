using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class CreateAnswerOptionRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    [Range(1, 10000)]
    public int SortOrder { get; set; }
}
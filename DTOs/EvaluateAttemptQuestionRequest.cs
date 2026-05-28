using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class EvaluateAttemptQuestionRequest
{
    [Required]
    public bool? IsCorrect { get; set; }
}
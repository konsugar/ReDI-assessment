using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class EvaluateAttemptQuestionRequest
{
    public bool? IsCorrect { get; set; }
}
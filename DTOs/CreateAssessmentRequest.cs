using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class CreateAssessmentRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 10000)]
    public int? TimeLimitMinutes { get; set; }
}
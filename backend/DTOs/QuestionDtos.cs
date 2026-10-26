using System.ComponentModel.DataAnnotations;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.DTOs;

public class QuestionSummaryResponse
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public int AnswerOptionCount { get; set; }
}

public class CreateQuestionRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [EnumDataType(typeof(QuestionType))]
    public QuestionType QuestionType { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }
}

public class UpdateQuestionRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [EnumDataType(typeof(QuestionType))]
    public QuestionType QuestionType { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }
}
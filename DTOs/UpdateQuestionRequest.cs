using System.ComponentModel.DataAnnotations;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.DTOs;

public class UpdateQuestionRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [EnumDataType(typeof(QuestionType))]
    public QuestionType QuestionType { get; set; }

    [Range(1, 10000)]
    public int SortOrder { get; set; }
}
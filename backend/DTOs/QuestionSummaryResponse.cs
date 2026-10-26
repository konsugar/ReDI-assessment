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
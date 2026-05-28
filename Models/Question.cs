namespace ReDI.Assessment.Api.Models;

public class Question
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }

    public Assessment? Assessment { get; set; }

    public string Text { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; }

    public int SortOrder { get; set; }

    public List<AnswerOption> AnswerOptions { get; set; } = new();
}
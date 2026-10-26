namespace ReDI.Assessment.Api.Models;

public class AttemptAnswerOption
{
    public int Id { get; set; }

    public int AttemptQuestionId { get; set; }

    public AttemptQuestion? AttemptQuestion { get; set; }

    public int AnswerOptionId { get; set; }

    public AnswerOption? AnswerOption { get; set; }
}
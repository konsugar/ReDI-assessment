namespace ReDI.Assessment.Api.DTOs;

public class SubmitAttemptQuestionAnswerRequest
{
    public string? AnswerText { get; set; }

    public List<int> SelectedAnswerOptionIds { get; set; } = new();
}
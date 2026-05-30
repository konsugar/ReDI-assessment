namespace ReDI.Assessment.Api.DTOs;

public class FrontendQuestionsResponse
{
    public List<FrontendTopicResponse> Topics { get; set; } = new();

    public List<FrontendQuizQuestionResponse> Questions { get; set; } = new();

    public List<FrontendOpenQuestionResponse> OpenQuestions { get; set; } = new();
}

public class FrontendTopicResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class FrontendQuizQuestionResponse
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public List<string> Options { get; set; } = new();

    // CorrectIndex is intentionally not returned by the real backend.
    // The frontend should send the selected option, and scoring happens on the server.
}

public class FrontendOpenQuestionResponse
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;
}
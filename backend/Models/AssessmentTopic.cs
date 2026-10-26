namespace ReDI.Assessment.Api.Models;

public class AssessmentTopic
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }

    public Assessment Assessment { get; set; } = null!;

    public int TopicId { get; set; }

    public Topic Topic { get; set; } = null!;

    public int SortOrder { get; set; }
}
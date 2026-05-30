using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class AssessmentTopicResponse
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }

    public int TopicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public class CreateAssessmentTopicRequest
{
    [Range(1, int.MaxValue)]
    public int TopicId { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }
}

public class UpdateAssessmentTopicRequest
{
    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }
}
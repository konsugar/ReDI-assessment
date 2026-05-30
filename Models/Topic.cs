using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.Models;

public class Topic
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public List<AssessmentTopic> AssessmentTopics { get; set; } = new();
}
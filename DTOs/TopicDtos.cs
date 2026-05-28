using System.ComponentModel.DataAnnotations;

namespace ReDI.Assessment.Api.DTOs;

public class TopicResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public class CreateTopicRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;
}
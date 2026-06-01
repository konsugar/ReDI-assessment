namespace ReDI.Assessment.Api.DTOs;

public class UpdateAssessmentPublicationRequest
{
    // Publication is changed only through the dedicated publication endpoint.
    public bool IsPublished { get; set; }
}
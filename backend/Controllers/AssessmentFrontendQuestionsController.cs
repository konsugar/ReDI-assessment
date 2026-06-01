using Microsoft.AspNetCore.Mvc;

using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Services;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/assessments/{assessmentId:int}/frontend-questions")]
public class AssessmentFrontendQuestionsController : ControllerBase
{
    private readonly FrontendQuestionsResponseService _frontendQuestionsResponseService;

    public AssessmentFrontendQuestionsController(
        FrontendQuestionsResponseService frontendQuestionsResponseService)
    {
        _frontendQuestionsResponseService = frontendQuestionsResponseService;
    }

    [HttpGet]
    public async Task<ActionResult<FrontendQuestionsResponse>> GetFrontendQuestions(
        int assessmentId)
    {
        var result = await _frontendQuestionsResponseService.BuildForLaunchAsync(assessmentId);

        if (result.Status == FrontendQuestionsBuildStatus.Ok)
        {
            return result.Response!;
        }

        if (result.Status == FrontendQuestionsBuildStatus.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.Status == FrontendQuestionsBuildStatus.NotReady)
        {
            return BadRequest(new
            {
                message = result.Error,
                readinessIssues = result.ReadinessIssues
            });
        }

        return BadRequest(result.Error);
    }

}
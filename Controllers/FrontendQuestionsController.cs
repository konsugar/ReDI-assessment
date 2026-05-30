using Microsoft.AspNetCore.Mvc;

using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Services;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/questions")]
public class FrontendQuestionsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly FrontendQuestionsResponseService _frontendQuestionsResponseService;

    public FrontendQuestionsController(
    IConfiguration configuration,
    FrontendQuestionsResponseService frontendQuestionsResponseService)
    {
        _configuration = configuration;
        _frontendQuestionsResponseService = frontendQuestionsResponseService;
    }

    [HttpGet]
    public async Task<ActionResult<FrontendQuestionsResponse>> GetQuestions()
    {
        var assessmentId = _configuration.GetValue<int>("FrontendAssessment:AssessmentId");

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

    private int GetFrontendAssessmentId()
    {
        return _configuration.GetValue<int>("FrontendAssessment:AssessmentId");
    }
}
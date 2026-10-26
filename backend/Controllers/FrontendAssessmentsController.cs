using Microsoft.AspNetCore.Mvc;

using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Services;

using Microsoft.AspNetCore.Authorization;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Authorize]

[Route("api/frontend-assessments")]
public class FrontendAssessmentsController : ControllerBase
{
    private readonly FrontendAssessmentSummaryService _frontendAssessmentSummaryService;

    public FrontendAssessmentsController(
        FrontendAssessmentSummaryService frontendAssessmentSummaryService)
    {
        _frontendAssessmentSummaryService = frontendAssessmentSummaryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FrontendAssessmentSummaryResponse>>> GetFrontendAssessments()
    {
        var assessments = await _frontendAssessmentSummaryService.GetSummariesAsync();

        return assessments;
    }

    [HttpGet("current")]
    public async Task<ActionResult<FrontendAssessmentSummaryResponse>> GetCurrentFrontendAssessment()
    {
        var assessment = await _frontendAssessmentSummaryService.GetCurrentAsync();

        if (assessment == null)
        {
            return NotFound();
        }

        return assessment;
    }

    [AllowAnonymous]
    [HttpGet("launchable")]
    public async Task<ActionResult<List<FrontendAssessmentSummaryResponse>>> GetLaunchableFrontendAssessments()
    {
        var assessments = await _frontendAssessmentSummaryService.GetLaunchableAsync();

        return assessments;
    }


    [HttpGet("{assessmentId:int}")]
    public async Task<ActionResult<FrontendAssessmentSummaryResponse>> GetFrontendAssessment(
    int assessmentId)
    {
        var assessment = await _frontendAssessmentSummaryService.GetByIdAsync(assessmentId);

        if (assessment == null)
        {
            return NotFound();
        }

        return assessment;
    }

}
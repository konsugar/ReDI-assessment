using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;
using ReDI.Assessment.Api.Services;

using Microsoft.AspNetCore.Authorization;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Authorize]

[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly SubmissionResponseService _submissionResponseService;
    private readonly OpenAnswerReviewService _openAnswerReviewService;
    private readonly FrontendSubmissionService _frontendSubmissionService;

    public SubmissionsController(
        AssessmentDbContext context,
        IConfiguration configuration,
        SubmissionResponseService submissionResponseService,
        OpenAnswerReviewService openAnswerReviewService,
        FrontendSubmissionService frontendSubmissionService)
    {
        _context = context;
        _configuration = configuration;
        _submissionResponseService = submissionResponseService;
        _openAnswerReviewService = openAnswerReviewService;
        _frontendSubmissionService = frontendSubmissionService;
    }

    private int GetFrontendAssessmentId()
    {
        return _configuration.GetValue<int>("FrontendAssessment:AssessmentId");
    }


    [HttpGet]
    public async Task<ActionResult<List<AdminSubmissionSummaryResponse>>> GetSubmissions()
    {
        var assessmentId = GetFrontendAssessmentId();

        var attempts = await _context.AssessmentAttempts
            .Include(attempt => attempt.SelfEvaluations)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.SelectedAnswerOptions)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.Question)
                    .ThenInclude(question => question.AnswerOptions)
            .Where(attempt =>
                attempt.AssessmentId == assessmentId &&
                attempt.SubmittedUtc != null)
            .OrderByDescending(attempt => attempt.SubmittedUtc)
            .ToListAsync();

        var response = attempts
            .Select(attempt => _submissionResponseService.BuildSummary(attempt))
            .ToList();

        return Ok(response);
    }


    [HttpGet("details")]
    public async Task<ActionResult<List<AdminSubmissionDetailsResponse>>> GetSubmissionDetails()
    {
        var assessmentId = GetFrontendAssessmentId();

        var attempts = await _context.AssessmentAttempts
            .Include(attempt => attempt.SelfEvaluations)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.SelectedAnswerOptions)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.Question)
                    .ThenInclude(question => question.AnswerOptions)
            .Where(attempt =>
                attempt.AssessmentId == assessmentId &&
                attempt.SubmittedUtc != null)
            .OrderByDescending(attempt => attempt.SubmittedUtc)
            .ToListAsync();

        var response = attempts
            .Select(attempt => _submissionResponseService.BuildDetails(attempt))
            .ToList();

        return Ok(response);
    }


    [HttpGet("{attemptId:int}")]
    public async Task<ActionResult<AdminSubmissionDetailsResponse>> GetSubmissionById(int attemptId)
    {
        var assessmentId = GetFrontendAssessmentId();

        var attempt = await _context.AssessmentAttempts
            .Include(attempt => attempt.SelfEvaluations)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.SelectedAnswerOptions)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.Question)
                    .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(attempt =>
                attempt.Id == attemptId &&
                attempt.AssessmentId == assessmentId &&
                attempt.SubmittedUtc != null);

        if (attempt == null)
        {
            return NotFound();
        }

        var response = _submissionResponseService.BuildDetails(attempt);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<FrontendSubmissionResponse>> CreateSubmission(
    FrontendSubmissionRequest request)
    {
        var assessmentId = GetFrontendAssessmentId();

        var result = await _frontendSubmissionService.CreateAsync(
            assessmentId,
            request);

        if (result.Status == FrontendSubmissionCreateStatus.Created)
        {
            return Created(
                $"/api/submissions/{result.AttemptId}",
                result.Response);
        }

        if (result.Status == FrontendSubmissionCreateStatus.NotFound)
        {
            return NotFound(result.Error);
        }

        if (result.Status == FrontendSubmissionCreateStatus.NotReady)
        {
            return BadRequest(new
            {
                message = result.Error,
                readinessIssues = result.ReadinessIssues
            });
        }

        return BadRequest(result.Error);
    }


    [HttpPut("{submissionId:int}/open-answers/{attemptQuestionId:int}/evaluation")]
    public async Task<IActionResult> EvaluateOpenAnswer(
    int submissionId,
    int attemptQuestionId,
    EvaluateAttemptQuestionRequest request)
    {
        if (!await SubmissionBelongsToCurrentAssessmentAsync(submissionId))
        {
            return NotFound();
        }

        var result = await _openAnswerReviewService.EvaluateAsync(
            submissionId,
            attemptQuestionId,
            request.IsCorrect);

        if (result.Success)
        {
            return NoContent();
        }

        if (result.Error == "NotFound")
        {
            return NotFound();
        }

        return BadRequest(result.Error);
    }


    [HttpPut("{submissionId:int}/open-answers/{attemptQuestionId:int}/teacher-note")]
    public async Task<IActionResult> UpdateOpenAnswerTeacherNote(
    int submissionId,
    int attemptQuestionId,
    UpdateTeacherNoteRequest request)
    {
        if (!await SubmissionBelongsToCurrentAssessmentAsync(submissionId))
        {
            return NotFound();
        }

        var result = await _openAnswerReviewService.UpdateTeacherNoteAsync(
            submissionId,
            attemptQuestionId,
            request.TeacherNote);

        if (result.Success)
        {
            return NoContent();
        }

        if (result.Error == "NotFound")
        {
            return NotFound();
        }

        return BadRequest(result.Error);
    }


    private async Task<bool> SubmissionBelongsToCurrentAssessmentAsync(int submissionId)
    {
        var assessmentId = _configuration.GetValue<int>("FrontendAssessment:AssessmentId");

        return await _context.AssessmentAttempts
            .AnyAsync(attempt =>
                attempt.Id == submissionId &&
                attempt.AssessmentId == assessmentId);
    }

}
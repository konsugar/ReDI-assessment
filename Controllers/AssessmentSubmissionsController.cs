using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Services;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/assessments/{assessmentId:int}/submissions")]
public class AssessmentSubmissionsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly SubmissionResponseService _submissionResponseService;
    private readonly OpenAnswerReviewService _openAnswerReviewService;

    public AssessmentSubmissionsController(
        AssessmentDbContext context,
        SubmissionResponseService submissionResponseService,
        OpenAnswerReviewService openAnswerReviewService)
    {
        _context = context;
        _submissionResponseService = submissionResponseService;
        _openAnswerReviewService = openAnswerReviewService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminSubmissionSummaryResponse>>> GetSubmissions(int assessmentId)
    {
        var assessmentExists = await _context.Assessments
            .AnyAsync(assessment => assessment.Id == assessmentId);

        if (!assessmentExists)
        {
            return NotFound();
        }

        var attempts = await LoadSubmittedAttempts(assessmentId);

        var response = attempts
            .Select(attempt => _submissionResponseService.BuildSummary(attempt))
            .ToList();

        return Ok(response);
    }

    [HttpGet("details")]
    public async Task<ActionResult<List<AdminSubmissionDetailsResponse>>> GetSubmissionDetails(int assessmentId)
    {
        var assessmentExists = await _context.Assessments
            .AnyAsync(assessment => assessment.Id == assessmentId);

        if (!assessmentExists)
        {
            return NotFound();
        }

        var attempts = await LoadSubmittedAttempts(assessmentId);

        var response = attempts
            .Select(attempt => _submissionResponseService.BuildDetails(attempt))
            .ToList();

        return Ok(response);
    }

    [HttpGet("{attemptId:int}")]
    public async Task<ActionResult<AdminSubmissionDetailsResponse>> GetSubmissionById(
        int assessmentId,
        int attemptId)
    {
        var attempt = await LoadSubmittedAttempt(assessmentId, attemptId);

        if (attempt == null)
        {
            return NotFound();
        }

        var response = _submissionResponseService.BuildDetails(attempt);

        return Ok(response);
    }


    [HttpPut("{submissionId:int}/open-answers/{attemptQuestionId:int}/evaluation")]
    public async Task<IActionResult> EvaluateOpenAnswer(
    int assessmentId,
    int submissionId,
    int attemptQuestionId,
    EvaluateAttemptQuestionRequest request)
    {
        if (!await SubmissionBelongsToAssessmentAsync(assessmentId, submissionId))
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
    int assessmentId,
    int submissionId,
    int attemptQuestionId,
    UpdateTeacherNoteRequest request)
    {
        if (!await SubmissionBelongsToAssessmentAsync(assessmentId, submissionId))
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


    private async Task<List<Models.AssessmentAttempt>> LoadSubmittedAttempts(int assessmentId)
    {
        return await _context.AssessmentAttempts
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
    }

    private async Task<Models.AssessmentAttempt?> LoadSubmittedAttempt(
        int assessmentId,
        int attemptId)
    {
        return await _context.AssessmentAttempts
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
    }

    private async Task<bool> SubmissionBelongsToAssessmentAsync(
    int assessmentId,
    int submissionId)
    {
        return await _context.AssessmentAttempts
            .AnyAsync(attempt =>
                attempt.Id == submissionId &&
                attempt.AssessmentId == assessmentId);
    }

}
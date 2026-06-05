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

[Route("api/assessments/{assessmentId:int}/attempts")]
public class AssessmentAttemptsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly AttemptStatusService _attemptStatusService;
    private readonly FrontendAssessmentReadinessService _readinessService;

    public AssessmentAttemptsController(
        AssessmentDbContext context,
        AttemptStatusService attemptStatusService,
        FrontendAssessmentReadinessService readinessService)
    {
        _context = context;
        _attemptStatusService = attemptStatusService;
        _readinessService = readinessService;
    }


    [HttpGet]
    public async Task<IActionResult> GetAttempts(int assessmentId)
    {
        var currentUtc = DateTime.UtcNow;

        //var attempts = await _context.AssessmentAttempts
        //    .Where(a => a.AssessmentId == assessmentId)
        //    .OrderByDescending(a => a.StartedUtc)
        //    .Select(a => new AssessmentAttemptResponse
        //    {
        //        Id = a.Id,
        //        AssessmentId = a.AssessmentId,
        //        StartedUtc = a.StartedUtc,
        //        EndsUtc = a.EndsUtc,
        //        CurrentUtc = currentUtc,
        //        RemainingSeconds = a.EndsUtc == null || a.SubmittedUtc != null
        //            ? null
        //            : Math.Max(0, (int)(a.EndsUtc.Value - currentUtc).TotalSeconds),
        //        Status = a.SubmittedUtc != null
        //            ? "Submitted"
        //            : a.EndsUtc != null && currentUtc > a.EndsUtc
        //                ? "Expired"
        //                : "InProgress",
        //        SubmittedUtc = a.SubmittedUtc,
        //        IsSubmitted = a.SubmittedUtc != null,
        //        IsExpired = a.SubmittedUtc == null &&
        //            a.EndsUtc != null &&
        //            currentUtc > a.EndsUtc,
        //        QuestionCount = a.AttemptQuestions.Count
        //    })
        //    .ToListAsync();

        //return Ok(attempts);


        var attempts = await _context.AssessmentAttempts
            .Where(attempt => attempt.AssessmentId == assessmentId)
            .Include(attempt => attempt.AttemptQuestions)
            .OrderByDescending(attempt => attempt.StartedUtc)
            .ToListAsync();

        var response = attempts
            .Select(attempt =>
            {
                var statusInfo = _attemptStatusService.GetStatus(attempt);

                return new AssessmentAttemptResponse
                {
                    Id = attempt.Id,
                    AssessmentId = attempt.AssessmentId,
                    StartedUtc = attempt.StartedUtc,
                    EndsUtc = attempt.EndsUtc,
                    CurrentUtc = statusInfo.CurrentUtc,
                    RemainingSeconds = statusInfo.RemainingSeconds,
                    Status = statusInfo.Status,
                    SubmittedUtc = attempt.SubmittedUtc,
                    IsSubmitted = statusInfo.IsSubmitted,
                    IsExpired = statusInfo.IsExpired,
                    QuestionCount = attempt.AttemptQuestions.Count
                };
            })
            .ToList();

        return Ok(response);
    }


    [HttpPost]
    public async Task<IActionResult> StartAttempt(int assessmentId)
    {
        var assessmentExists = await _context.Assessments
            .AnyAsync(a => a.Id == assessmentId);

        if (!assessmentExists)
        {
            return NotFound();
        }

        // check (before start) that assessment contains at least one question
        
        var questionIds = await _context.Questions
            .Where(q => q.AssessmentId == assessmentId)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .Select(q => q.Id)
            .ToListAsync();
        
        if (!questionIds.Any())
        {
            return BadRequest("Assessment must contain at least one question before starting an attempt.");
        }

        // check (before start) that choice questions have at least one answer option

        var choiceQuestionsWithoutAnswerOptions = await _context.Questions
            .Where(q =>
                q.AssessmentId == assessmentId &&
                (q.QuestionType == QuestionType.SingleChoice ||
                    q.QuestionType == QuestionType.MultipleChoice) &&
                !q.AnswerOptions.Any())
            .AnyAsync();

        
        if (choiceQuestionsWithoutAnswerOptions)
        {
            return BadRequest("All choice questions must have answer options before starting an attempt.");
        }

        // check (before start) that choice questions have at least one correct answer option

        var choiceQuestionsWithoutCorrectAnswer = await _context.Questions
            .Where(q =>
                q.AssessmentId == assessmentId &&
                (q.QuestionType == QuestionType.SingleChoice ||
                    q.QuestionType == QuestionType.MultipleChoice) &&
                !q.AnswerOptions.Any(ao => ao.IsCorrect))
            .AnyAsync();

        if (choiceQuestionsWithoutCorrectAnswer)
        {
            return BadRequest("All choice questions must have at least one correct answer option before starting an attempt.");
        }

        // check (before start) that single choice questions have only one correct answer option

        var singleChoiceQuestionsWithMultipleCorrectAnswers = await _context.Questions
            .Where(q =>
                q.AssessmentId == assessmentId &&
                q.QuestionType == QuestionType.SingleChoice &&
                q.AnswerOptions.Count(ao => ao.IsCorrect) > 1)
            .AnyAsync();

        if (singleChoiceQuestionsWithMultipleCorrectAnswers)
        {
            return BadRequest("Single choice questions must have exactly one correct answer option before starting an attempt.");
        }


        var assessment = await _context.Assessments
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(assessment => assessment.Id == assessmentId);

        if (assessment == null)
        {
            return NotFound();
        }

        if (!assessment.IsPublished)
        {
            return BadRequest("Assessment is not published.");
        }

        var readinessIssues = _readinessService.GetReadinessIssues(assessment);

        if (readinessIssues.Count > 0)
        {
            return BadRequest(new
            {
                message = "Assessment is not ready for frontend.",
                readinessIssues
            });
        }

        // the moment of attempt's creation
        var startedUtc = DateTime.UtcNow;

        var attempt = new AssessmentAttempt
        {
            AssessmentId = assessmentId,
            StartedUtc = startedUtc,
            EndsUtc = assessment.TimeLimitMinutes == null
                ? null
                : startedUtc.AddMinutes(assessment.TimeLimitMinutes.Value)
        };


        _context.AssessmentAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        var statusInfo = _attemptStatusService.GetStatus(attempt);

        var attemptQuestions = questionIds.Select(questionId => new AttemptQuestion
        {
            AssessmentAttemptId = attempt.Id,
            QuestionId = questionId
        }).ToList();

        _context.AttemptQuestions.AddRange(attemptQuestions);
        await _context.SaveChangesAsync();

        var currentUtc = DateTime.UtcNow;

        var response = new AssessmentAttemptResponse
        {
            Id = attempt.Id,
            AssessmentId = attempt.AssessmentId,
            StartedUtc = attempt.StartedUtc,
            EndsUtc = attempt.EndsUtc,
            CurrentUtc = statusInfo.CurrentUtc,
            RemainingSeconds = statusInfo.RemainingSeconds,
            Status = statusInfo.Status,
            IsSubmitted = statusInfo.IsSubmitted,
            IsExpired = statusInfo.IsExpired,

            QuestionCount = attemptQuestions.Count
        };

        return CreatedAtAction(
            nameof(AttemptsController.GetAttemptById),
            "Attempts",
            new { attemptId = response.Id },
            response);
    }

}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;
using ReDI.Assessment.Api.Services;

namespace ReDI.Assessment.Api.Controllers;

// general attempts controller

[ApiController]
[Route("api/attempts")]
public class AttemptsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly AttemptStatusService _attemptStatusService;
    private readonly OpenAnswerReviewService _openAnswerReviewService;

    public AttemptsController(
    AssessmentDbContext context,
    AttemptStatusService attemptStatusService,
    OpenAnswerReviewService openAnswerReviewService)
    {
        _context = context;
        _attemptStatusService = attemptStatusService;
        _openAnswerReviewService = openAnswerReviewService;
    }

    [HttpGet("{attemptId:int}")]
    public async Task<IActionResult> GetAttemptById(int attemptId)
    {
        var attempt = await _context.AssessmentAttempts
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.Question)
                    .ThenInclude(q => q.AnswerOptions)
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.SelectedAnswerOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
        {
            return NotFound();
        }

        var statusInfo = _attemptStatusService.GetStatus(attempt);

        var response = new AttemptDetailsResponse
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
            Questions = attempt.AttemptQuestions
                .OrderBy(aq => aq.Question!.SortOrder)
                .ThenBy(aq => aq.QuestionId)
                .Select(aq => new AttemptQuestionResponse
                {
                    AttemptQuestionId = aq.Id,
                    QuestionId = aq.QuestionId,
                    Text = aq.Question!.Text,
                    QuestionType = aq.Question.QuestionType.ToString(),
                    SortOrder = aq.Question.SortOrder,
                    AnswerText = aq.AnswerText,

                    SelectedAnswerOptionIds = aq.SelectedAnswerOptions
                        .Select(aao => aao.AnswerOptionId)
                        .OrderBy(id => id)
                        .ToList(),
                    AnswerOptions = aq.Question.AnswerOptions
                        .OrderBy(ao => ao.SortOrder)
                        .ThenBy(ao => ao.Id)
                        .Select(ao => new AttemptAnswerOptionResponse
                        {
                            Id = ao.Id,
                            Text = ao.Text,
                            SortOrder = ao.SortOrder
                        })
                        .ToList()
                })
                .ToList()
        };

        return Ok(response);
    }


    [HttpGet("{attemptId:int}/status")]
    public async Task<IActionResult> GetAttemptStatus(int attemptId)
    {
        var attempt = await _context.AssessmentAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
        {
            return NotFound();
        }

        var statusInfo = _attemptStatusService.GetStatus(attempt);

        var response = new AttemptStatusResponse
        {
            AttemptId = attempt.Id,
            AssessmentId = attempt.AssessmentId,
            StartedUtc = attempt.StartedUtc,
            EndsUtc = attempt.EndsUtc,
            CurrentUtc = statusInfo.CurrentUtc,
            RemainingSeconds = statusInfo.RemainingSeconds,
            Status = statusInfo.Status,
            SubmittedUtc = attempt.SubmittedUtc,
            IsSubmitted = statusInfo.IsSubmitted,
            IsExpired = statusInfo.IsExpired
        };

        return Ok(response);
    }


    // submit answer - main method

    [HttpPut("{attemptId:int}/questions/{attemptQuestionId:int}/answer")]
    public async Task<IActionResult> SubmitQuestionAnswer(
    int attemptId,
    int attemptQuestionId,
    SubmitAttemptQuestionAnswerRequest request)
    {
        var attemptQuestion = await _context.AttemptQuestions
            .Include(aq => aq.AssessmentAttempt)
            .Include(aq => aq.Question)
            .FirstOrDefaultAsync(aq =>
                aq.Id == attemptQuestionId &&
                aq.AssessmentAttemptId == attemptId);

        if (attemptQuestion == null)
        {
            return NotFound();
        }

        if (attemptQuestion.AssessmentAttempt.SubmittedUtc != null)
        {
            return BadRequest("This attempt has already been submitted.");
        }

        // Submitting after EndsUtc is allowed.
        // Answers cannot be changed after EndsUtc, but already saved answers can still be finalized.

        if (attemptQuestion.AssessmentAttempt.EndsUtc != null &&
            DateTime.UtcNow > attemptQuestion.AssessmentAttempt.EndsUtc)
        {
            return BadRequest("This attempt has expired.");
        }

        // no more than 1 answer in multiple choice

        if (request.SelectedAnswerOptionIds.Count != request.SelectedAnswerOptionIds.Distinct().Count())
        {
            return BadRequest("Selected answer option ids must be unique.");
        }

        if (attemptQuestion.Question.QuestionType != QuestionType.SingleChoice &&
            attemptQuestion.Question.QuestionType != QuestionType.MultipleChoice &&
            request.SelectedAnswerOptionIds.Any())
        {
            return BadRequest("Selected answer options are allowed only for choice questions.");
        }

        if ((attemptQuestion.Question.QuestionType == QuestionType.SingleChoice ||
            attemptQuestion.Question.QuestionType == QuestionType.MultipleChoice) &&
            !string.IsNullOrWhiteSpace(request.AnswerText))
        {
            return BadRequest("Answer text is not allowed for choice questions.");
        }

        // only 1 answer in single choice

        if (attemptQuestion.Question.QuestionType == QuestionType.SingleChoice &&
            request.SelectedAnswerOptionIds.Distinct().Count() > 1)
        {
            return BadRequest("Single choice questions can have only one selected answer option.");
        }

        var validAnswerOptionIds = await _context.AnswerOptions
            .Where(ao => ao.QuestionId == attemptQuestion.QuestionId)
            .Select(ao => ao.Id)
            .ToListAsync();

        var hasInvalidSelectedOption = request.SelectedAnswerOptionIds
            .Any(id => !validAnswerOptionIds.Contains(id));

        if (hasInvalidSelectedOption)
        {
            return BadRequest("One or more selected answer options do not belong to this question.");
        }

        var existingSelectedOptions = await _context.AttemptAnswerOptions
            .Where(aao => aao.AttemptQuestionId == attemptQuestionId)
            .ToListAsync();

        _context.AttemptAnswerOptions.RemoveRange(existingSelectedOptions);

        var newSelectedOptions = request.SelectedAnswerOptionIds
            .Distinct()
            .Select(answerOptionId => new AttemptAnswerOption
            {
                AttemptQuestionId = attemptQuestionId,
                AnswerOptionId = answerOptionId
            })
            .ToList();

        _context.AttemptAnswerOptions.AddRange(newSelectedOptions);

        attemptQuestion.AnswerText = request.AnswerText;

        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpPut("{attemptId:int}/questions/{attemptQuestionId:int}/evaluation")]
    public async Task<IActionResult> EvaluateAttemptQuestion(
    int attemptId,
    int attemptQuestionId,
    EvaluateAttemptQuestionRequest request)
    {
        var result = await _openAnswerReviewService.EvaluateAsync(
            attemptId,
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


    [HttpPut("{attemptId:int}/questions/{attemptQuestionId:int}/teacher-note")]
    public async Task<IActionResult> UpdateTeacherNote(
    int attemptId,
    int attemptQuestionId,
    UpdateTeacherNoteRequest request)
    {
        var result = await _openAnswerReviewService.UpdateTeacherNoteAsync(
            attemptId,
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


    [HttpPost("{attemptId:int}/submit")]
    public async Task<IActionResult> SubmitAttempt(int attemptId)
    {
        var attempt = await _context.AssessmentAttempts
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.Question)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
        {
            return NotFound();
        }

        if (attempt.SubmittedUtc != null)
        {
            return BadRequest("This attempt has already been submitted.");
        }



        var attemptQuestionIds = attempt.AttemptQuestions
            .Select(aq => aq.Id)
            .ToList();

        var questionIds = attempt.AttemptQuestions
            .Select(aq => aq.QuestionId)
            .ToList();

        var selectedAnswerOptions = await _context.AttemptAnswerOptions
            .Where(aao => attemptQuestionIds.Contains(aao.AttemptQuestionId))
            .ToListAsync();

        var correctAnswerOptions = await _context.AnswerOptions
            .Where(ao => questionIds.Contains(ao.QuestionId) && ao.IsCorrect)
            .ToListAsync();

        var automaticallyCheckedQuestionCount = 0;

        //var correctQuestionCount = 0;

        var correctQuestionCount = attempt.AttemptQuestions
            .Count(attemptQuestion => attemptQuestion.IsCorrect == true);

        foreach (var attemptQuestion in attempt.AttemptQuestions)
        {
            var questionType = attemptQuestion.Question!.QuestionType;

            if (questionType != QuestionType.SingleChoice &&
                questionType != QuestionType.MultipleChoice)
            {
                attemptQuestion.IsCorrect = null;
                continue;
            }

            automaticallyCheckedQuestionCount++;

            var selectedIds = selectedAnswerOptions
                .Where(aao => aao.AttemptQuestionId == attemptQuestion.Id)
                .Select(aao => aao.AnswerOptionId)
                .OrderBy(id => id)
                .ToList();

            var correctIds = correctAnswerOptions
                .Where(ao => ao.QuestionId == attemptQuestion.QuestionId)
                .Select(ao => ao.Id)
                .OrderBy(id => id)
                .ToList();

            var isCorrect = correctIds.Count > 0 &&
                selectedIds.SequenceEqual(correctIds);

            attemptQuestion.IsCorrect = isCorrect;

            if (isCorrect)
            {
                correctQuestionCount++;
            }
        }

        var submittedUtc = DateTime.UtcNow;
        attempt.SubmittedUtc = submittedUtc;

        await _context.SaveChangesAsync();

        var statusInfo = _attemptStatusService.GetStatus(attempt);

        var response = new SubmitAttemptResponse
        {
            AttemptId = attempt.Id,
            AssessmentId = attempt.AssessmentId,
            EndsUtc = attempt.EndsUtc,

            CurrentUtc = statusInfo.CurrentUtc,
            RemainingSeconds = statusInfo.RemainingSeconds,
            Status = statusInfo.Status,
            SubmittedUtc = submittedUtc,
            IsSubmitted = statusInfo.IsSubmitted,
            IsExpired = statusInfo.IsExpired,

            QuestionCount = attempt.AttemptQuestions.Count,
            AutomaticallyCheckedQuestionCount = automaticallyCheckedQuestionCount,
            CorrectQuestionCount = correctQuestionCount
        };

        return Ok(response);
    }

    // result endpoint for an attempt, which includes the attempt details along with the correctness of each question and the selected answer options

    [HttpGet("{attemptId:int}/result")]
    public async Task<IActionResult> GetAttemptResult(int attemptId)
    {
        var attempt = await _context.AssessmentAttempts
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.Question)
            .Include(a => a.AttemptQuestions)
                .ThenInclude(aq => aq.SelectedAnswerOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
        {
            return NotFound();
        }

        if (attempt.SubmittedUtc == null)
        {
            return BadRequest("This attempt has not been submitted yet.");
        }

        //var attemptQuestionIds = attempt.AttemptQuestions
        //    .Select(aq => aq.Id)
        //    .ToList();

        //var selectedAnswerOptions = await _context.AttemptAnswerOptions
        //    .Where(aao => attemptQuestionIds.Contains(aao.AttemptQuestionId))
        //    .ToListAsync();

        var automaticallyCheckedQuestionCount = attempt.AttemptQuestions
            .Count(aq => aq.IsCorrect != null);

        var correctQuestionCount = attempt.AttemptQuestions
            .Count(aq => aq.IsCorrect == true);

        var statusInfo = _attemptStatusService.GetStatus(attempt);

        var response = new AttemptResultResponse
        {
            AttemptId = attempt.Id,
            AssessmentId = attempt.AssessmentId,
            StartedUtc = attempt.StartedUtc,
            EndsUtc = attempt.EndsUtc,

            CurrentUtc = statusInfo.CurrentUtc,
            RemainingSeconds = statusInfo.RemainingSeconds,
            Status = statusInfo.Status,
            IsSubmitted = statusInfo.IsSubmitted,
            IsExpired = statusInfo.IsExpired,
            
            QuestionCount = attempt.AttemptQuestions.Count,
            AutomaticallyCheckedQuestionCount = automaticallyCheckedQuestionCount,
            CorrectQuestionCount = correctQuestionCount,
            Questions = attempt.AttemptQuestions
                .OrderBy(aq => aq.Question.SortOrder)
                .ThenBy(aq => aq.QuestionId)
                .Select(aq => new AttemptQuestionResultResponse
                {
                    AttemptQuestionId = aq.Id,
                    QuestionId = aq.QuestionId,
                    Text = aq.Question.Text,
                    QuestionType = aq.Question.QuestionType.ToString(),
                    SortOrder = aq.Question.SortOrder,
                    AnswerText = aq.AnswerText,
                    SelectedAnswerOptionIds = aq.SelectedAnswerOptions
                        .Select(aao => aao.AnswerOptionId)
                        .OrderBy(id => id)
                        .ToList(),
                    IsCorrect = aq.IsCorrect
                })
                .ToList()
        };

        return Ok(response);
    }

}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;

using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/assessments/{assessmentId:int}/questions")]
public class QuestionsController : ControllerBase
{
    private readonly AssessmentDbContext _context;

    public QuestionsController(AssessmentDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuestions(int assessmentId)
    {
        var assessmentExists = await _context.Assessments
            .AnyAsync(a => a.Id == assessmentId);

        if (!assessmentExists)
        {
            return NotFound();
        }

        var questions = await _context.Questions
            .Where(q => q.AssessmentId == assessmentId)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .Select(q => new QuestionSummaryResponse
            {
                Id = q.Id,
                AssessmentId = q.AssessmentId,
                Text = q.Text,
                QuestionType = q.QuestionType.ToString(),
                SortOrder = q.SortOrder,
                AnswerOptionCount = q.AnswerOptions.Count
            })
            .ToListAsync();

        return Ok(questions);
    }

    [HttpGet("{questionId:int}")]
    public async Task<ActionResult<QuestionSummaryResponse>> GetQuestionById(
    int assessmentId,
    int questionId)
    {
        var question = await _context.Questions
            .Include(question => question.AnswerOptions)
            .FirstOrDefaultAsync(question =>
                question.Id == questionId &&
                question.AssessmentId == assessmentId);

        if (question == null)
        {
            return NotFound();
        }

        var response = new QuestionSummaryResponse
        {
            Id = question.Id,
            AssessmentId = question.AssessmentId,
            Text = question.Text,
            QuestionType = question.QuestionType.ToString(),
            SortOrder = question.SortOrder,
            AnswerOptionCount = question.AnswerOptions.Count
        };

        return Ok(response);
    }


    [HttpPost]
    public async Task<IActionResult> CreateQuestion(int assessmentId, CreateQuestionRequest request)
    {
        var assessmentExists = await _context.Assessments
            .AnyAsync(a => a.Id == assessmentId);

        if (!assessmentExists)
        {
            return NotFound();
        }


        var assessmentHasAttempts = await _context.AssessmentAttempts
             .AnyAsync(a => a.AssessmentId == assessmentId);

        if (assessmentHasAttempts)
        {
            return BadRequest("Questions cannot be added after attempts have been started for this assessment.");
        }

        if (!IsSupportedMvpQuestionType(request.QuestionType))
        {
            return BadRequest("This question type is not supported in the MVP. Use SingleChoice, MultipleChoice, or TextInput.");
        }

        var question = new Question
        {
            AssessmentId = assessmentId,
            Text = request.Text,
            QuestionType = request.QuestionType,
            SortOrder = request.SortOrder
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        var response = new QuestionSummaryResponse
        {
            Id = question.Id,
            AssessmentId = question.AssessmentId,
            Text = question.Text,
            QuestionType = question.QuestionType.ToString(),
            SortOrder = question.SortOrder,
            AnswerOptionCount = 0
        };

        return CreatedAtAction(
            nameof(GetQuestionById),
            new
            {
                assessmentId = response.AssessmentId,
                questionId = response.Id
            },
            response);
    }


    [HttpPut("{questionId:int}")]
    public async Task<IActionResult> UpdateQuestion(
    int assessmentId,
    int questionId,
    UpdateQuestionRequest request)
    {
        var question = await _context.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == questionId &&
                q.AssessmentId == assessmentId);

        if (question == null)
        {
            return NotFound();
        }


        var assessmentHasAttempts = await _context.AssessmentAttempts
            .AnyAsync(a => a.AssessmentId == assessmentId);

        if (assessmentHasAttempts)
        {
            return BadRequest("Questions cannot be edited after attempts have been started for this assessment.");
        }


        if (!IsSupportedMvpQuestionType(request.QuestionType))
        {
            return BadRequest("This question type is not supported in the MVP. Use SingleChoice, MultipleChoice, or TextInput.");
        }


        var existingAnswerOptions = await _context.AnswerOptions
            .Where(ao => ao.QuestionId == question.Id)
            .ToListAsync();

        var newTypeIsChoice =
            request.QuestionType == QuestionType.SingleChoice ||
            request.QuestionType == QuestionType.MultipleChoice;

        if (!newTypeIsChoice && existingAnswerOptions.Any())
        {
            return BadRequest("Question type cannot be changed to a non-choice type while answer options exist.");
        }

        if (request.QuestionType == QuestionType.SingleChoice &&
            existingAnswerOptions.Count(ao => ao.IsCorrect) > 1)
        {
            return BadRequest("Single choice questions can have only one correct answer option.");
        }


        question.Text = request.Text;
        question.QuestionType = request.QuestionType;
        question.SortOrder = request.SortOrder;

        await _context.SaveChangesAsync();

        var answerOptionCount = await _context.AnswerOptions
            .CountAsync(ao => ao.QuestionId == question.Id);

        var response = new QuestionSummaryResponse
        {
            Id = question.Id,
            AssessmentId = question.AssessmentId,
            Text = question.Text,
            QuestionType = question.QuestionType.ToString(),
            SortOrder = question.SortOrder,
            AnswerOptionCount = answerOptionCount
        };

        return Ok(response);
    }


    [HttpDelete("{questionId:int}")]
    public async Task<IActionResult> DeleteQuestion(int assessmentId, int questionId)
    {
        var question = await _context.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == questionId &&
                q.AssessmentId == assessmentId);

        if (question == null)
        {
            return NotFound();
        }

        var assessmentHasAttempts = await _context.AssessmentAttempts
            .AnyAsync(a => a.AssessmentId == assessmentId);

        if (assessmentHasAttempts)
        {
            return BadRequest("Questions cannot be deleted after attempts have been started for this assessment.");
        }

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static bool IsSupportedMvpQuestionType(QuestionType questionType)
    {
        // MVP supports only choice questions and free-text answers.
        // NumericInput and Slider are intentionally blocked for now.
        return questionType is QuestionType.SingleChoice
            or QuestionType.MultipleChoice
            or QuestionType.TextInput;
    }

}
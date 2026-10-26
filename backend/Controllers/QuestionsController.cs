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

[Route("api/assessments/{assessmentId:int}/questions")]
public class QuestionsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly AssessmentStructureGuardService _structureGuardService;

    public QuestionsController(
    AssessmentDbContext context,
    AssessmentStructureGuardService structureGuardService)
    {
        _context = context;
        _structureGuardService = structureGuardService;
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
        var structureValidationResult = await ValidateStructureCanBeEditedAsync(assessmentId);

        if (structureValidationResult != null)
        {
            return structureValidationResult;
        }

        var assessmentExists = await _context.Assessments
            .AnyAsync(a => a.Id == assessmentId);

        if (!assessmentExists)
        {
            return NotFound();
        }

        if (!IsSupportedMvpQuestionType(request.QuestionType))
        {
            return BadRequest("This question type is not supported in the MVP. Use SingleChoice, MultipleChoice, or TextInput.");
        }

        var text = request.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest("Question text is required.");
        }

        var question = new Question
        {
            AssessmentId = assessmentId,
            Text = text,
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
        var structureValidationResult = await ValidateStructureCanBeEditedAsync(assessmentId);

        if (structureValidationResult != null)
        {
            return structureValidationResult;
        }

        var question = await _context.Questions
            .FirstOrDefaultAsync(q =>
                q.Id == questionId &&
                q.AssessmentId == assessmentId);

        if (question == null)
        {
            return NotFound();
        }

        if (!IsSupportedMvpQuestionType(request.QuestionType))
        {
            return BadRequest("This question type is not supported in the MVP. Use SingleChoice, MultipleChoice, or TextInput.");
        }

        var text = request.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest("Question text is required.");
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


        question.Text = text;
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
        var structureValidationResult = await ValidateStructureCanBeEditedAsync(assessmentId);

        if (structureValidationResult != null)
        {
            return structureValidationResult;
        }

        var question = await _context.Questions
            .Include(question => question.AnswerOptions)
            .FirstOrDefaultAsync(question =>
        question.Id == questionId &&
        question.AssessmentId == assessmentId);

        if (question == null)
        {
            return NotFound();
        }

        _context.AnswerOptions.RemoveRange(question.AnswerOptions);
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


    private async Task<IActionResult?> ValidateStructureCanBeEditedAsync(int assessmentId)
    {
        var result = await _structureGuardService.CanEditAssessmentStructureAsync(assessmentId);

        if (result.CanEdit)
        {
            return null;
        }

        if (result.Error == "Assessment was not found.")
        {
            return NotFound(result.Error);
        }

        return BadRequest(result.Error);
    }

}
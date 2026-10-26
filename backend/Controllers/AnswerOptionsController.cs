using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;

namespace ReDI.Assessment.Api.Controllers;

using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;
using ReDI.Assessment.Api.Services;

using Microsoft.AspNetCore.Authorization;

[ApiController]
[Authorize]

[Route("api/questions/{questionId:int}/answer-options")]
public class AnswerOptionsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly AssessmentStructureGuardService _structureGuardService;

    public AnswerOptionsController(
    AssessmentDbContext context,
    AssessmentStructureGuardService structureGuardService)
    {
        _context = context;
        _structureGuardService = structureGuardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnswerOptions(int questionId)
    {
        var questionExists = await _context.Questions
            .AnyAsync(q => q.Id == questionId);

        if (!questionExists)
        {
            return NotFound();
        }

        var answerOptions = await _context.AnswerOptions
            .Where(ao => ao.QuestionId == questionId)
            .OrderBy(ao => ao.SortOrder)
            .ThenBy(ao => ao.Id)
            .Select(ao => new AnswerOptionResponse
            {
                Id = ao.Id,
                Text = ao.Text,
                IsCorrect = ao.IsCorrect,
                SortOrder = ao.SortOrder
            })
            .ToListAsync();

        return Ok(answerOptions);
    }


    [HttpGet("{answerOptionId:int}")]
    public async Task<ActionResult<AnswerOptionResponse>> GetAnswerOptionById(
    int questionId,
    int answerOptionId)
    {
        var answerOption = await _context.AnswerOptions
            .FirstOrDefaultAsync(answerOption =>
                answerOption.Id == answerOptionId &&
                answerOption.QuestionId == questionId);

        if (answerOption == null)
        {
            return NotFound();
        }

        var response = new AnswerOptionResponse
        {
            Id = answerOption.Id,
            Text = answerOption.Text,
            IsCorrect = answerOption.IsCorrect,
            SortOrder = answerOption.SortOrder
        };

        return Ok(response);
    }


    [HttpPost]
    public async Task<IActionResult> CreateAnswerOption(int questionId, CreateAnswerOptionRequest request)
    {
        var structureValidationResult = await ValidateQuestionStructureCanBeEditedAsync(questionId);

        if (structureValidationResult != null)
        {
            return structureValidationResult;
        }

        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null)
        {
            return NotFound();
        }

        if (question.QuestionType != QuestionType.SingleChoice &&
            question.QuestionType != QuestionType.MultipleChoice)
        {
            return BadRequest("Answer options are allowed only for choice questions.");
        }

        if (question.QuestionType == QuestionType.SingleChoice && request.IsCorrect)
        {
            var alreadyHasCorrectAnswer = await _context.AnswerOptions
                .AnyAsync(ao => ao.QuestionId == questionId && ao.IsCorrect);

            if (alreadyHasCorrectAnswer)
            {
                return BadRequest("Single choice questions can have only one correct answer option.");
            }
        }

        var text = request.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest("Answer option text is required.");
        }

        var answerOption = new AnswerOption
        {
            QuestionId = questionId,
            Text = text,
            IsCorrect = request.IsCorrect,
            SortOrder = request.SortOrder
        };

        _context.AnswerOptions.Add(answerOption);
        await _context.SaveChangesAsync();

        var response = new AnswerOptionResponse
        {
            Id = answerOption.Id,
            Text = answerOption.Text,
            IsCorrect = answerOption.IsCorrect,
            SortOrder = answerOption.SortOrder
        };

        return CreatedAtAction(
            nameof(GetAnswerOptionById),
            new
            {
                questionId,
                answerOptionId = response.Id
            },
            response);
    }


    [HttpPut("{answerOptionId:int}")]
    public async Task<IActionResult> UpdateAnswerOption(
        int questionId,
        int answerOptionId,
        UpdateAnswerOptionRequest request)
    {
        var structureValidationResult = await ValidateQuestionStructureCanBeEditedAsync(questionId);

        if (structureValidationResult != null)
        {
            return structureValidationResult;
        }

        var answerOption = await _context.AnswerOptions
            .Include(ao => ao.Question)
            .FirstOrDefaultAsync(ao =>
                ao.Id == answerOptionId &&
                ao.QuestionId == questionId);

        if (answerOption == null)
        {
            return NotFound();
        }

        if (answerOption.Question.QuestionType == QuestionType.SingleChoice && request.IsCorrect)
        {
            var anotherCorrectAnswerExists = await _context.AnswerOptions
                .AnyAsync(ao =>
                    ao.QuestionId == questionId &&
                    ao.Id != answerOptionId &&
                    ao.IsCorrect);

            if (anotherCorrectAnswerExists)
            {
                return BadRequest("Single choice questions can have only one correct answer option.");
            }
        }


        if ((answerOption.Question.QuestionType == QuestionType.SingleChoice ||
            answerOption.Question.QuestionType == QuestionType.MultipleChoice) &&
            answerOption.IsCorrect &&
            !request.IsCorrect)
        {
            var anotherCorrectAnswerExists = await _context.AnswerOptions
                .AnyAsync(ao =>
                    ao.QuestionId == questionId &&
                    ao.Id != answerOptionId &&
                    ao.IsCorrect);

            if (!anotherCorrectAnswerExists)
            {
                return BadRequest("Choice questions must have at least one correct answer option.");
            }
        }

        var text = request.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest("Answer option text is required.");
        }

        answerOption.Text = text;
        answerOption.IsCorrect = request.IsCorrect;
        answerOption.SortOrder = request.SortOrder;

        await _context.SaveChangesAsync();

        var response = new AnswerOptionResponse
        {
            Id = answerOption.Id,
            Text = answerOption.Text,
            IsCorrect = answerOption.IsCorrect,
            SortOrder = answerOption.SortOrder
        };

        return Ok(response);
    }


    [HttpDelete("{answerOptionId:int}")]
    public async Task<IActionResult> DeleteAnswerOption(int questionId, int answerOptionId)
    {
        var structureValidationResult = await ValidateQuestionStructureCanBeEditedAsync(questionId);

        if (structureValidationResult != null)
        {
            return structureValidationResult;
        }

        var answerOption = await _context.AnswerOptions
            .Include(ao => ao.Question)
            .FirstOrDefaultAsync(ao =>
                ao.Id == answerOptionId &&
                ao.QuestionId == questionId);

        if (answerOption == null)
        {
            return NotFound();
        }

        //var assessmentHasAttempts = await _context.AssessmentAttempts
        //    .AnyAsync(a => a.AssessmentId == answerOption.Question.AssessmentId);

        //if (assessmentHasAttempts)
        //{
        //    return BadRequest("Answer options cannot be deleted after attempts have been started for this assessment.");
        //}

        if ((answerOption.Question.QuestionType == QuestionType.SingleChoice ||
            answerOption.Question.QuestionType == QuestionType.MultipleChoice) &&
            answerOption.IsCorrect)
        {
            var anotherCorrectAnswerExists = await _context.AnswerOptions
                .AnyAsync(ao =>
                    ao.QuestionId == questionId &&
                    ao.Id != answerOptionId &&
                    ao.IsCorrect);

            if (!anotherCorrectAnswerExists)
            {
                return BadRequest("Choice questions must have at least one correct answer option.");
            }
        }

        _context.AnswerOptions.Remove(answerOption);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    private async Task<IActionResult?> ValidateQuestionStructureCanBeEditedAsync(int questionId)
    {
        var result = await _structureGuardService.CanEditQuestionStructureAsync(questionId);

        if (result.CanEdit)
        {
            return null;
        }

        if (result.Error == "Question was not found.")
        {
            return NotFound(result.Error);
        }

        if (result.Error == "Assessment was not found.")
        {
            return NotFound(result.Error);
        }

        return BadRequest(result.Error);
    }

}
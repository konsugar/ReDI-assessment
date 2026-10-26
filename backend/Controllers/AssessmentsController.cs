using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;
using ReDI.Assessment.Api.Services;

using AssessmentEntity = ReDI.Assessment.Api.Models.Assessment;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssessmentsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly FrontendAssessmentReadinessService _readinessService;
    private readonly AssessmentContentService _assessmentContentService;

    public AssessmentsController(
    AssessmentDbContext context,
    FrontendAssessmentReadinessService readinessService,
    AssessmentContentService assessmentContentService   )
    {
        _context = context;
        _readinessService = readinessService;
        _assessmentContentService = assessmentContentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAssessments()
    {
        var assessments = await _context.Assessments
            .OrderBy(a => a.Id)
            .Select(a => new AssessmentSummaryResponse
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                TimeLimitMinutes = a.TimeLimitMinutes,
                CreatedUtc = a.CreatedUtc,
                QuestionCount = a.Questions.Count
            })
            .ToListAsync();

        return Ok(assessments);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAssessmentById(int id)
    {
        var assessment = await _context.Assessments
            .Include(a => a.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assessment == null)
        {
            return NotFound();
        }

        var response = new AssessmentDetailsResponse
        {
            Id = assessment.Id,
            Title = assessment.Title,
            Description = assessment.Description,
            TimeLimitMinutes = assessment.TimeLimitMinutes,
            CreatedUtc = assessment.CreatedUtc,
            IsPublished = assessment.IsPublished,
            Questions = assessment.Questions
                .OrderBy(q => q.SortOrder)
                .ThenBy(q => q.Id)
                .Select(q => new QuestionResponse
                {
                    Id = q.Id,
                    Text = q.Text,
                    QuestionType = q.QuestionType.ToString(),
                    SortOrder = q.SortOrder,
                    AnswerOptions = q.AnswerOptions
                        .OrderBy(ao => ao.SortOrder)
                        .ThenBy(ao => ao.Id)
                        .Select(ao => new AnswerOptionResponse
                        {
                            Id = ao.Id,
                            Text = ao.Text,
                            IsCorrect = ao.IsCorrect,
                            SortOrder = ao.SortOrder
                        })
                        .ToList()
                })
                .ToList()
        };

        return Ok(response);
    }


    [HttpPost]
    public async Task<IActionResult> CreateAssessment(CreateAssessmentRequest request)
    {
        var title = request.Title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest("Assessment title is required.");
        }

        var description = request.Description?.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            description = null;
        }

        var assessment = new AssessmentEntity
        {
            Title = title,
            Description = description,
            TimeLimitMinutes = request.TimeLimitMinutes,
            CreatedUtc = DateTime.UtcNow,
            IsPublished = false
        };

        _context.Assessments.Add(assessment);
        await _context.SaveChangesAsync();

        var response = new AssessmentSummaryResponse
        {
            Id = assessment.Id,
            Title = assessment.Title,
            Description = assessment.Description,
            TimeLimitMinutes = assessment.TimeLimitMinutes,
            CreatedUtc = assessment.CreatedUtc,
            IsPublished = assessment.IsPublished,
            QuestionCount = 0
        };

        return CreatedAtAction(
            nameof(GetAssessmentById),
            new { id = response.Id },
            response);
    }

    // Clone = Creates a new editable assessment.
    // Copying: the assessment metadata, topics, questions, and answer options from the source.
    // Not copying: Attempts, submitted answers, self-evaluations, and evaluation results.

    [HttpPost("{id:int}/clone")]
    public async Task<ActionResult<AssessmentSummaryResponse>> CloneAssessment(int id)
    {
        var sourceAssessment = await _context.Assessments
            .Include(assessment => assessment.Topics)
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(assessment => assessment.Id == id);

        if (sourceAssessment == null)
        {
            return NotFound();
        }

        var clonedAssessment = new AssessmentEntity
        {
            Title = $"{sourceAssessment.Title} (copy)",
            Description = sourceAssessment.Description,
            TimeLimitMinutes = sourceAssessment.TimeLimitMinutes,
            CreatedUtc = DateTime.UtcNow,
            IsPublished = false,
            Topics = sourceAssessment.Topics
                .OrderBy(topic => topic.SortOrder)
                .ThenBy(topic => topic.Id)
                .Select(topic => new AssessmentTopic
                {
                    TopicId = topic.TopicId,
                    SortOrder = topic.SortOrder
                })
                .ToList(),
            Questions = sourceAssessment.Questions
                .OrderBy(question => question.SortOrder)
                .ThenBy(question => question.Id)
                .Select(question => new Question
                {
                    Text = question.Text,
                    QuestionType = question.QuestionType,
                    SortOrder = question.SortOrder,
                    AnswerOptions = question.AnswerOptions
                        .OrderBy(answerOption => answerOption.SortOrder)
                        .ThenBy(answerOption => answerOption.Id)
                        .Select(answerOption => new AnswerOption
                        {
                            Text = answerOption.Text,
                            IsCorrect = answerOption.IsCorrect,
                            SortOrder = answerOption.SortOrder
                        })
                        .ToList()
                })
                .ToList()
        };

        _context.Assessments.Add(clonedAssessment);
        await _context.SaveChangesAsync();

        var response = new AssessmentSummaryResponse
        {
            Id = clonedAssessment.Id,
            Title = clonedAssessment.Title,
            Description = clonedAssessment.Description,
            TimeLimitMinutes = clonedAssessment.TimeLimitMinutes,
            CreatedUtc = clonedAssessment.CreatedUtc,
            QuestionCount = clonedAssessment.Questions.Count,
            IsPublished = clonedAssessment.IsPublished
        };

        return CreatedAtAction(
            nameof(GetAssessmentById),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAssessment(int id, UpdateAssessmentRequest request)
    {
        var assessment = await _context.Assessments
            .Include(assessment => assessment.Questions)
            .Include(assessment => assessment.Attempts)
            .FirstOrDefaultAsync(assessment => assessment.Id == id);

        if (assessment == null)
        {
            return NotFound();
        }

        var title = request.Title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest("Assessment title is required.");
        }

        var description = request.Description?.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            description = null;
        }

        var timeLimitChanged = assessment.TimeLimitMinutes != request.TimeLimitMinutes;

        if (timeLimitChanged && assessment.IsPublished)
        {
            return BadRequest("Assessment time limit cannot be changed while it is published. Unpublish it first.");
        }

        if (timeLimitChanged && assessment.Attempts.Count > 0)
        {
            return BadRequest($"Assessment time limit cannot be changed because it already has {assessment.Attempts.Count} submission(s).");
        }

        assessment.Title = title;
        assessment.Description = description;
        assessment.TimeLimitMinutes = request.TimeLimitMinutes;

        await _context.SaveChangesAsync();

        var response = new AssessmentSummaryResponse
        {
            Id = assessment.Id,
            Title = assessment.Title,
            Description = assessment.Description,
            TimeLimitMinutes = assessment.TimeLimitMinutes,
            CreatedUtc = assessment.CreatedUtc,
            QuestionCount = assessment.Questions.Count,
            IsPublished = assessment.IsPublished
        };

        return Ok(response);
    }


    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAssessment(int id)
    {
        var assessment = await _context.Assessments
            .Include(assessment => assessment.Attempts)
            .Include(assessment => assessment.Topics)
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(assessment => assessment.Id == id);

        if (assessment == null)
        {
            return NotFound();
        }

        if (assessment.IsPublished)
        {
            return BadRequest("Assessment cannot be deleted while it is published. Unpublish it first.");
        }

        if (assessment.Attempts.Count > 0)
        {
            return BadRequest("Assessment cannot be deleted after attempts have been started.");
        }

        var answerOptions = assessment.Questions
            .SelectMany(question => question.AnswerOptions)
            .ToList();

        _context.AnswerOptions.RemoveRange(answerOptions);
        _context.Questions.RemoveRange(assessment.Questions);
        _context.AssessmentTopics.RemoveRange(assessment.Topics);
        _context.Assessments.Remove(assessment);
        
        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpPut("{id:int}/publication")]
    public async Task<IActionResult> UpdateAssessmentPublication(
    int id,
    UpdateAssessmentPublicationRequest request)
    {
        var assessment = await _context.Assessments
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(assessment => assessment.Id == id);

        if (assessment == null)
        {
            return NotFound();
        }

        if (request.IsPublished)
        {
            var readinessIssues = _readinessService.GetReadinessIssues(assessment);

            if (readinessIssues.Count > 0)
            {
                return BadRequest(new
                {
                    message = "Assessment cannot be published because it is not ready for frontend.",
                    readinessIssues
                });
            }
        }

        assessment.IsPublished = request.IsPublished;

        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpGet("{id:int}/content")]
    public async Task<ActionResult<AssessmentContentResponse>> GetAssessmentContent(int id)
    {
        var content = await _assessmentContentService.GetContentAsync(id);

        if (content == null)
        {
            return NotFound();
        }

        return content;
    }

}
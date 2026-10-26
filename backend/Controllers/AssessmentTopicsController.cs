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

[Route("api/assessments/{assessmentId:int}/topics")]
public class AssessmentTopicsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly AssessmentStructureGuardService _structureGuardService;

    public AssessmentTopicsController(
    AssessmentDbContext context,
    AssessmentStructureGuardService structureGuardService)
    {
        _context = context;
        _structureGuardService = structureGuardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AssessmentTopicResponse>>> GetTopics(int assessmentId)
    {
        var assessmentExists = await _context.Assessments
            .AnyAsync(assessment => assessment.Id == assessmentId);

        if (!assessmentExists)
        {
            return NotFound();
        }

        var response = await _context.AssessmentTopics
            .Include(assessmentTopic => assessmentTopic.Topic)
            .Where(assessmentTopic => assessmentTopic.AssessmentId == assessmentId)
            .OrderBy(assessmentTopic => assessmentTopic.SortOrder)
            .ThenBy(assessmentTopic => assessmentTopic.Id)
            .Select(assessmentTopic => new AssessmentTopicResponse
            {
                Id = assessmentTopic.Id,
                AssessmentId = assessmentTopic.AssessmentId,
                TopicId = assessmentTopic.TopicId,
                Name = assessmentTopic.Topic.Name,
                Description = assessmentTopic.Topic.Description,
                SortOrder = assessmentTopic.SortOrder
            })
            .ToListAsync();

        return Ok(response);
    }

    [HttpGet("{assessmentTopicId:int}")]
    public async Task<ActionResult<AssessmentTopicResponse>> GetTopicById(
        int assessmentId,
        int assessmentTopicId)
    {
        var assessmentTopic = await _context.AssessmentTopics
            .Include(assessmentTopic => assessmentTopic.Topic)
            .FirstOrDefaultAsync(assessmentTopic =>
                assessmentTopic.Id == assessmentTopicId &&
                assessmentTopic.AssessmentId == assessmentId);

        if (assessmentTopic == null)
        {
            return NotFound();
        }

        var response = new AssessmentTopicResponse
        {
            Id = assessmentTopic.Id,
            AssessmentId = assessmentTopic.AssessmentId,
            TopicId = assessmentTopic.TopicId,
            Name = assessmentTopic.Topic.Name,
            Description = assessmentTopic.Topic.Description,
            SortOrder = assessmentTopic.SortOrder
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<AssessmentTopicResponse>> CreateTopic(
        int assessmentId,
        CreateAssessmentTopicRequest request)
    {
        var structureValidationResult = await ValidateStructureCanBeEditedAsync(assessmentId);

        if (structureValidationResult != null)
        {
            return structureValidationResult;
        }

        var assessmentExists = await _context.Assessments
            .AnyAsync(assessment => assessment.Id == assessmentId);

        if (!assessmentExists)
        {
            return NotFound();
        }

        var topic = await _context.Topics
            .FirstOrDefaultAsync(topic => topic.Id == request.TopicId);

        if (topic == null)
        {
            return BadRequest("Topic does not exist.");
        }

        var topicAlreadyLinked = await _context.AssessmentTopics
            .AnyAsync(assessmentTopic =>
                assessmentTopic.AssessmentId == assessmentId &&
                assessmentTopic.TopicId == request.TopicId);

        if (topicAlreadyLinked)
        {
            return BadRequest("This topic is already linked to the assessment.");
        }

        var assessmentTopic = new AssessmentTopic
        {
            AssessmentId = assessmentId,
            TopicId = request.TopicId,
            SortOrder = request.SortOrder
        };

        _context.AssessmentTopics.Add(assessmentTopic);
        await _context.SaveChangesAsync();

        var response = new AssessmentTopicResponse
        {
            Id = assessmentTopic.Id,
            AssessmentId = assessmentTopic.AssessmentId,
            TopicId = assessmentTopic.TopicId,
            Name = topic.Name,
            Description = topic.Description,
            SortOrder = assessmentTopic.SortOrder
        };

        return CreatedAtAction(
            nameof(GetTopicById),
            new
            {
                assessmentId = response.AssessmentId,
                assessmentTopicId = response.Id
            },
            response);
    }

    [HttpPut("{assessmentTopicId:int}")]
    public async Task<IActionResult> UpdateAssessmentTopic(
    int assessmentId,
    int assessmentTopicId,
    UpdateAssessmentTopicRequest request)
    {
        var structureValidationResult = await ValidateStructureCanBeEditedAsync(assessmentId);

        if (structureValidationResult != null)
        {
            return structureValidationResult;
        }

        var assessmentTopic = await _context.AssessmentTopics
            .FirstOrDefaultAsync(assessmentTopic =>
                assessmentTopic.Id == assessmentTopicId &&
                assessmentTopic.AssessmentId == assessmentId);

        if (assessmentTopic == null)
        {
            return NotFound();
        }

        assessmentTopic.SortOrder = request.SortOrder;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{assessmentTopicId:int}")]
    public async Task<IActionResult> RemoveTopicFromAssessment(
    int assessmentId,
    int assessmentTopicId)
    {
        var structureValidationResult = await ValidateStructureCanBeEditedAsync(assessmentId);

        if (structureValidationResult != null)
        {
            return structureValidationResult;
        }

        var assessmentTopic = await _context.AssessmentTopics
            .FirstOrDefaultAsync(assessmentTopic =>
                assessmentTopic.Id == assessmentTopicId &&
                assessmentTopic.AssessmentId == assessmentId);

        if (assessmentTopic == null)
        {
            return NotFound();
        }

        _context.AssessmentTopics.Remove(assessmentTopic);

        await _context.SaveChangesAsync();

        return NoContent();
    }


    private async Task<ActionResult?> ValidateStructureCanBeEditedAsync(int assessmentId)
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
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
public class TopicsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly AssessmentStructureGuardService _structureGuardService;

    public TopicsController(
        AssessmentDbContext context,
        AssessmentStructureGuardService structureGuardService)
    {
        _context = context;
        _structureGuardService = structureGuardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TopicResponse>>> GetTopics()
    {
        var response = await _context.Topics
            .OrderBy(topic => topic.Name)
            .ThenBy(topic => topic.Id)
            .Select(topic => new TopicResponse
            {
                Id = topic.Id,
                Name = topic.Name,
                Description = topic.Description
            })
            .ToListAsync();

        return Ok(response);
    }

    [HttpGet("{topicId:int}")]
    public async Task<ActionResult<TopicResponse>> GetTopicById(int topicId)
    {
        var topic = await _context.Topics
            .FirstOrDefaultAsync(topic => topic.Id == topicId);

        if (topic == null)
        {
            return NotFound();
        }

        var response = new TopicResponse
        {
            Id = topic.Id,
            Name = topic.Name,
            Description = topic.Description
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TopicResponse>> CreateTopic(CreateTopicRequest request)
    {
        var trimmedName = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return BadRequest("Topic name is required.");
        }

        var description = request.Description?.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            description = null;
        }

        var topicExists = await _context.Topics
            .AnyAsync(topic => topic.Name == trimmedName);

        if (topicExists)
        {
            return BadRequest("Topic with this name already exists.");
        }

        var topic = new Topic
        {
            Name = trimmedName,
            Description = description
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();

        var response = new TopicResponse
        {
            Id = topic.Id,
            Name = topic.Name,
            Description = topic.Description
        };

        return CreatedAtAction(
            nameof(GetTopicById),
            new { topicId = response.Id },
            response);
    }

    [HttpPut("{topicId:int}")]
    public async Task<IActionResult> UpdateTopic(
    int topicId,
    UpdateTopicRequest request)
    {
        var topicValidationResult = await ValidateGlobalTopicCanBeEditedAsync(topicId);

        if (topicValidationResult != null)
        {
            return topicValidationResult;
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Topic name is required.");
        }

        var duplicateTopicExists = await _context.Topics
            .AnyAsync(topic =>
                topic.Id != topicId &&
                topic.Name == name);

        if (duplicateTopicExists)
        {
            return BadRequest("A topic with this name already exists.");
        }

        var topic = await _context.Topics
            .FirstOrDefaultAsync(topic => topic.Id == topicId);

        if (topic == null)
        {
            return NotFound();
        }

        topic.Name = name;
        topic.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{topicId:int}")]
    public async Task<IActionResult> DeleteTopic(int topicId)
    {
        var topic = await _context.Topics
            .FirstOrDefaultAsync(topic => topic.Id == topicId);

        if (topic == null)
        {
            return NotFound();
        }

        var isUsedByAnyAssessment = await _context.AssessmentTopics
            .AnyAsync(assessmentTopic => assessmentTopic.TopicId == topicId);

        if (isUsedByAnyAssessment)
        {
            return BadRequest("Topic cannot be deleted because it is used by one or more assessments. Remove it from those assessments first.");
        }

        _context.Topics.Remove(topic);

        await _context.SaveChangesAsync();

        return NoContent();
    }


    private async Task<ActionResult?> ValidateGlobalTopicCanBeEditedAsync(int topicId)
    {
        var result = await _structureGuardService.CanEditGlobalTopicAsync(topicId);

        if (result.CanEdit)
        {
            return null;
        }

        if (result.Error == "Topic was not found.")
        {
            return NotFound(result.Error);
        }

        return BadRequest(result.Error);
    }

}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TopicsController : ControllerBase
{
    private readonly AssessmentDbContext _context;

    public TopicsController(AssessmentDbContext context)
    {
        _context = context;
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
        var trimmedName = request.Name.Trim();

        var topicExists = await _context.Topics
            .AnyAsync(topic => topic.Name == trimmedName);

        if (topicExists)
        {
            return BadRequest("Topic with this name already exists.");
        }

        var topic = new Topic
        {
            Name = trimmedName,
            Description = request.Description.Trim()
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
}
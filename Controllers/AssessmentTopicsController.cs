using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/assessments/{assessmentId:int}/topics")]
public class AssessmentTopicsController : ControllerBase
{
    private readonly AssessmentDbContext _context;

    public AssessmentTopicsController(AssessmentDbContext context)
    {
        _context = context;
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

        var hasAttempts = await _context.AssessmentAttempts
            .AnyAsync(attempt => attempt.AssessmentId == assessmentId);

        if (hasAttempts)
        {
            return BadRequest("Topics cannot be added after attempts have been started for this assessment.");
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
}
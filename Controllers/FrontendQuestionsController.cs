using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/questions")]
public class FrontendQuestionsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly IConfiguration _configuration;

    public FrontendQuestionsController(
        AssessmentDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<FrontendQuestionsResponse>> GetQuestions()
    {
        var assessmentId = GetFrontendAssessmentId();

        var assessment = await _context.Assessments
            .Include(assessment => assessment.Topics)
                .ThenInclude(assessmentTopic => assessmentTopic.Topic)
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(assessment => assessment.Id == assessmentId);

        if (assessment == null)
        {
            return NotFound($"Frontend assessment with id {assessmentId} was not found.");
        }

        var response = new FrontendQuestionsResponse
        {
            Topics = assessment.Topics
                .OrderBy(assessmentTopic => assessmentTopic.SortOrder)
                .ThenBy(assessmentTopic => assessmentTopic.Id)
                .Select(assessmentTopic => new FrontendTopicResponse
                {
                    Id = assessmentTopic.TopicId,
                    Name = assessmentTopic.Topic.Name,
                    Description = assessmentTopic.Topic.Description
                })
                .ToList(),

            Questions = assessment.Questions
                .Where(question => question.QuestionType == QuestionType.SingleChoice)
                .OrderBy(question => question.SortOrder)
                .ThenBy(question => question.Id)
                .Select(question => new FrontendQuizQuestionResponse
                {
                    Id = question.Id,
                    Text = question.Text,
                    Options = question.AnswerOptions
                        .OrderBy(answerOption => answerOption.SortOrder)
                        .ThenBy(answerOption => answerOption.Id)
                        .Select(answerOption => answerOption.Text)
                        .ToList()
                })
                .ToList(),

            OpenQuestions = assessment.Questions
                .Where(question => question.QuestionType == QuestionType.TextInput)
                .OrderBy(question => question.SortOrder)
                .ThenBy(question => question.Id)
                .Select(question => new FrontendOpenQuestionResponse
                {
                    Id = question.Id,
                    Text = question.Text
                })
                .ToList()
        };

        return Ok(response);
    }

    private int GetFrontendAssessmentId()
    {
        return _configuration.GetValue<int>("FrontendAssessment:AssessmentId");
    }
}
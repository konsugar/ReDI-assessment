using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;

using AssessmentEntity = ReDI.Assessment.Api.Models.Assessment;

namespace ReDI.Assessment.Api.Services;

public class FrontendQuestionsResponseService
{
    private readonly AssessmentDbContext _context;
    private readonly FrontendAssessmentReadinessService _readinessService;

    public FrontendQuestionsResponseService(
        AssessmentDbContext context,
        FrontendAssessmentReadinessService readinessService)
    {
        _context = context;
        _readinessService = readinessService;
    }

    public async Task<FrontendQuestionsResponse?> BuildAsync(int assessmentId)
    {
        var assessment = await LoadAssessmentAsync(assessmentId);

        if (assessment == null)
        {
            return null;
        }

        return BuildResponse(assessment);
    }

    public async Task<FrontendQuestionsBuildResult> BuildForLaunchAsync(int assessmentId)
    {
        var assessment = await LoadAssessmentAsync(assessmentId);

        if (assessment == null)
        {
            return new FrontendQuestionsBuildResult
            {
                Status = FrontendQuestionsBuildStatus.NotFound,
                Error = $"Assessment with id {assessmentId} was not found."
            };
        }

        if (!assessment.IsPublished)
        {
            return new FrontendQuestionsBuildResult
            {
                Status = FrontendQuestionsBuildStatus.NotPublished,
                Error = "Assessment is not published."
            };
        }

        var readinessIssues = _readinessService.GetReadinessIssues(assessment);

        if (readinessIssues.Count > 0)
        {
            return new FrontendQuestionsBuildResult
            {
                Status = FrontendQuestionsBuildStatus.NotReady,
                Error = "Assessment is not ready for frontend.",
                ReadinessIssues = readinessIssues
            };
        }

        return new FrontendQuestionsBuildResult
        {
            Status = FrontendQuestionsBuildStatus.Ok,
            Response = BuildResponse(assessment)
        };
    }

    private async Task<AssessmentEntity?> LoadAssessmentAsync(int assessmentId)
    {
        return await _context.Assessments
            .Include(assessment => assessment.Topics)
                .ThenInclude(assessmentTopic => assessmentTopic.Topic)
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(assessment => assessment.Id == assessmentId);
    }

    private static FrontendQuestionsResponse BuildResponse(AssessmentEntity assessment)
    {
        return new FrontendQuestionsResponse
        {
            Topics = assessment.Topics
                .OrderBy(assessmentTopic => assessmentTopic.SortOrder)
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
                .Select(question => new FrontendQuizQuestionResponse
                {
                    Id = question.Id,
                    Text = question.Text,
                    Options = question.AnswerOptions
                        .OrderBy(answerOption => answerOption.SortOrder)
                        .Select(answerOption => answerOption.Text)
                        .ToList()
                })
                .ToList(),

            OpenQuestions = assessment.Questions
                .Where(question => question.QuestionType == QuestionType.TextInput)
                .OrderBy(question => question.SortOrder)
                .Select(question => new FrontendOpenQuestionResponse
                {
                    Id = question.Id,
                    Text = question.Text
                })
                .ToList()
        };
    }
}
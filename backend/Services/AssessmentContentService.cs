using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.Services;

public class AssessmentContentService
{
    private readonly AssessmentDbContext _context;
    private readonly FrontendAssessmentSummaryService _frontendAssessmentSummaryService;

    public AssessmentContentService(
        AssessmentDbContext context,
        FrontendAssessmentSummaryService frontendAssessmentSummaryService)
    {
        _context = context;
        _frontendAssessmentSummaryService = frontendAssessmentSummaryService;
    }

    public async Task<AssessmentContentResponse?> GetContentAsync(int assessmentId)
    {
        var assessment = await _context.Assessments
            .Include(assessment => assessment.Topics)
                .ThenInclude(assessmentTopic => assessmentTopic.Topic)
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(assessment => assessment.Id == assessmentId);

        if (assessment == null)
        {
            return null;
        }

        var status = await _frontendAssessmentSummaryService.GetByIdAsync(assessmentId);

        if (status == null)
        {
            return null;
        }


        var attachedTopicIds = assessment.Topics
            .Select(assessmentTopic => assessmentTopic.TopicId)
            .ToList();

        var availableTopics = await _context.Topics
            .Where(topic => !attachedTopicIds.Contains(topic.Id))
            .OrderBy(topic => topic.Name)
            .Select(topic => new AssessmentContentAvailableTopicResponse
            {
                Id = topic.Id,
                Name = topic.Name,
                Description = topic.Description
            })
            .ToListAsync();


        return new AssessmentContentResponse
        {
            Assessment = new AssessmentContentAssessmentResponse
            {
                Id = assessment.Id,
                Title = assessment.Title,
                Description = assessment.Description,
                TimeLimitMinutes = assessment.TimeLimitMinutes,
                IsPublished = assessment.IsPublished,
                CreatedUtc = assessment.CreatedUtc
            },
            Status = status,
            Topics = assessment.Topics
                .OrderBy(assessmentTopic => assessmentTopic.SortOrder)
                .ThenBy(assessmentTopic => assessmentTopic.Id)
                .Select(assessmentTopic => new AssessmentContentTopicResponse
                {
                    AssessmentTopicId = assessmentTopic.Id,
                    TopicId = assessmentTopic.TopicId,
                    Name = assessmentTopic.Topic.Name,
                    Description = assessmentTopic.Topic.Description,
                    SortOrder = assessmentTopic.SortOrder
                })
                .ToList(),
            Questions = assessment.Questions
                .OrderBy(question => question.SortOrder)
                .ThenBy(question => question.Id)
                .Select(question => new AssessmentContentQuestionResponse
                {
                    Id = question.Id,
                    Text = question.Text,
                    QuestionType = question.QuestionType,
                    QuestionTypeName = question.QuestionType.ToString(),
                    IsSupportedForFrontend = question.QuestionType == QuestionType.SingleChoice ||
                        question.QuestionType == QuestionType.TextInput,
                    SortOrder = question.SortOrder,
                    AnswerOptions = question.AnswerOptions
                        .OrderBy(answerOption => answerOption.SortOrder)
                        .ThenBy(answerOption => answerOption.Id)
                        .Select(answerOption => new AssessmentContentAnswerOptionResponse
                        {
                            Id = answerOption.Id,
                            QuestionId = answerOption.QuestionId,
                            Text = answerOption.Text,
                            IsCorrect = answerOption.IsCorrect,
                            SortOrder = answerOption.SortOrder
                        })
                        .ToList()
                })
                .ToList(),
            AvailableTopics = availableTopics,
            QuestionTypes = GetQuestionTypes()
        };
    }

    private static List<AssessmentContentQuestionTypeResponse> GetQuestionTypes()
    {
        return Enum.GetValues<QuestionType>()
            .Select(questionType => new AssessmentContentQuestionTypeResponse
            {
                Value = (int)questionType,
                Name = questionType.ToString(),
                IsSupportedForFrontend = questionType == QuestionType.SingleChoice ||
                    questionType == QuestionType.TextInput,
                UsesAnswerOptions = questionType == QuestionType.SingleChoice ||
                    questionType == QuestionType.MultipleChoice
            })
            .ToList();
    }


}
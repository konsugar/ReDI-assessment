using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;

namespace ReDI.Assessment.Api.Services;

public class AssessmentStructureGuardService
{
    private readonly AssessmentDbContext _context;

    public AssessmentStructureGuardService(AssessmentDbContext context)
    {
        _context = context;
    }

    public async Task<(bool CanEdit, string? Error)> CanEditAssessmentStructureAsync(
        int assessmentId)
    {
        var assessment = await _context.Assessments
            .Include(assessment => assessment.Attempts)
            .FirstOrDefaultAsync(assessment => assessment.Id == assessmentId);

        if (assessment == null)
        {
            return (false, "Assessment was not found.");
        }

        if (assessment.Attempts.Count > 0)
        {
            return (false, $"Assessment structure is locked because it already has {assessment.Attempts.Count} submission(s).");
        }

        if (assessment.IsPublished)
        {
            return (false, "Assessment structure is locked because it is published. Unpublish it before editing/deleting questions, answer options, or topics.");
        }

        return (true, null);
    }

    public async Task<(bool CanEdit, string? Error)> CanEditQuestionStructureAsync(
    int questionId)
    {
        var question = await _context.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(question => question.Id == questionId);

        if (question == null)
        {
            return (false, "Question was not found.");
        }

        return await CanEditAssessmentStructureAsync(question.AssessmentId);
    }

    public async Task<(bool CanEdit, string? Error)> CanEditGlobalTopicAsync(
    int topicId)
    {
        var topicExists = await _context.Topics
            .AnyAsync(topic => topic.Id == topicId);

        if (!topicExists)
        {
            return (false, "Topic was not found.");
        }

        var lockedAssessment = await _context.AssessmentTopics
            .Include(assessmentTopic => assessmentTopic.Assessment)
                .ThenInclude(assessment => assessment.Attempts)
            .Where(assessmentTopic => assessmentTopic.TopicId == topicId)
            .Select(assessmentTopic => assessmentTopic.Assessment)
            .FirstOrDefaultAsync(assessment =>
                assessment.IsPublished ||
                assessment.Attempts.Count > 0);

        if (lockedAssessment == null)
        {
            return (true, null);
        }

        if (lockedAssessment.Attempts.Count > 0)
        {
            return (false, $"Topic is locked because it is used by assessment {lockedAssessment.Id}, which already has {lockedAssessment.Attempts.Count} submission(s).");
        }

        return (false, $"Topic is locked because it is used by published assessment {lockedAssessment.Id}. Unpublish that assessment or remove the topic from it before editing the global topic.");
    }

}
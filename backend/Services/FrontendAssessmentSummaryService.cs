using Microsoft.EntityFrameworkCore;

using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;

using AssessmentEntity = ReDI.Assessment.Api.Models.Assessment;

namespace ReDI.Assessment.Api.Services;

public class FrontendAssessmentSummaryService
{
    private readonly AssessmentDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly FrontendAssessmentReadinessService _readinessService;

    public FrontendAssessmentSummaryService(
        AssessmentDbContext context,
        IConfiguration configuration,
        FrontendAssessmentReadinessService readinessService)
    {
        _context = context;
        _configuration = configuration;
        _readinessService = readinessService;
    }

    public async Task<List<FrontendAssessmentSummaryResponse>> GetSummariesAsync()
    {
        var currentAssessmentId = _configuration.GetValue<int>("FrontendAssessment:AssessmentId");

        var assessments = await _context.Assessments
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .Include(assessment => assessment.Topics)
            .Include(assessment => assessment.Attempts)
            .OrderByDescending(assessment => assessment.Id)
            .ToListAsync();

        return assessments
            .Select(assessment => BuildSummary(assessment, currentAssessmentId))
            .ToList();
    }

    public async Task<FrontendAssessmentSummaryResponse?> GetCurrentAsync()
    {
        var currentAssessmentId = _configuration.GetValue<int>("FrontendAssessment:AssessmentId");

        var assessment = await _context.Assessments
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .Include(assessment => assessment.Topics)
            .Include(assessment => assessment.Attempts)
            .FirstOrDefaultAsync(assessment => assessment.Id == currentAssessmentId);

        if (assessment == null)
        {
            return null;
        }

        return BuildSummary(assessment, currentAssessmentId);
    }

    public async Task<FrontendAssessmentSummaryResponse?> GetByIdAsync(int assessmentId)
    {
        var currentAssessmentId = _configuration.GetValue<int>("FrontendAssessment:AssessmentId");

        var assessment = await _context.Assessments
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .Include(assessment => assessment.Topics)
            .Include(assessment => assessment.Attempts)
            .FirstOrDefaultAsync(assessment => assessment.Id == assessmentId);

        if (assessment == null)
        {
            return null;
        }

        return BuildSummary(assessment, currentAssessmentId);
    }

    public async Task<List<FrontendAssessmentSummaryResponse>> GetLaunchableAsync()
    {
        var summaries = await GetSummariesAsync();

        return summaries
            .Where(assessment => assessment.CanLaunchFrontend)
            .ToList();
    }

    private FrontendAssessmentSummaryResponse BuildSummary(
        AssessmentEntity assessment,
        int currentAssessmentId)
    {
        var frontendQuestions = assessment.Questions
            .Where(question =>
                question.QuestionType == QuestionType.SingleChoice ||
                question.QuestionType == QuestionType.TextInput)
            .ToList();

        var readinessIssues = _readinessService.GetReadinessIssues(assessment);

        var submissionCount = assessment.Attempts.Count;
        var canEditStructure = submissionCount == 0 && !assessment.IsPublished;

        var isReadyForFrontend = readinessIssues.Count == 0;
        var canLaunchFrontend = assessment.IsPublished && isReadyForFrontend;

        return new FrontendAssessmentSummaryResponse
        {
            Id = assessment.Id,
            Title = assessment.Title,
            Description = assessment.Description,
            QuestionCount = frontendQuestions.Count,
            TopicCount = assessment.Topics.Count,
            SubmissionCount = submissionCount,
            CanEditStructure = canEditStructure,
            StructureLockReason = GetStructureLockReason(
                assessment.IsPublished,
                submissionCount),
            IsCurrent = assessment.Id == currentAssessmentId,
            IsPublished = assessment.IsPublished,
            IsReadyForFrontend = isReadyForFrontend,
            CanLaunchFrontend = canLaunchFrontend,
            ReadinessIssues = readinessIssues
        };


    }

    private static string? GetStructureLockReason(
    bool isPublished,
    int submissionCount)
    {
        if (submissionCount > 0)
        {
            return $"Assessment structure is locked because it already has {submissionCount} submission(s).";
        }

        if (isPublished)
        {
            return "Assessment structure is locked because it is published. Unpublish it before editing/deleting questions, answer options, or topics.";
        }

        return null;
    }


}
using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.Services;

public class FrontendSubmissionService
{
    private readonly AssessmentDbContext _context;
    private readonly SubmissionResponseService _submissionResponseService;
    private readonly FrontendAssessmentReadinessService _readinessService;

    public FrontendSubmissionService(
        AssessmentDbContext context,
        SubmissionResponseService submissionResponseService,
        FrontendAssessmentReadinessService readinessService)
    {
        _context = context;
        _submissionResponseService = submissionResponseService;
        _readinessService = readinessService;
    }

    public async Task<FrontendSubmissionCreateResult> CreateAsync(
        int assessmentId,
        FrontendSubmissionRequest request)
    {
        var participantCode = request.ParticipantCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(participantCode))
        {
            return BadRequest("Participant code is required.");
        }

        if (participantCode.Length > 100)
        {
            return BadRequest("Participant code must be 100 characters or fewer.");
        }

        if (request.SelfEvaluation == null)
        {
            return BadRequest("Self-evaluation is required.");
        }

        if (request.MultipleChoiceAnswers == null)
        {
            return BadRequest("Choice answers are required.");
        }

        if (request.OpenAnswers == null)
        {
            return BadRequest("Open answers are required.");
        }

        if (request.SelfEvaluation.Any(selfEvaluation => selfEvaluation == null))
        {
            return BadRequest("Self-evaluation must not contain empty items.");
        }

        if (request.MultipleChoiceAnswers.Any(answer => answer == null))
        {
            return BadRequest("Choice answers must not contain empty items.");
        }

        if (request.OpenAnswers.Any(answer => answer == null))
        {
            return BadRequest("Open answers must not contain empty items.");
        }

        if (request.OpenAnswers.Any(answer =>
            (answer?.Text?.Length ?? 0) > 4000))
        {
            return BadRequest("Open answer text must be 4000 characters or fewer.");
        }

        var assessment = await _context.Assessments
            .Include(assessment => assessment.Topics)
                .ThenInclude(assessmentTopic => assessmentTopic.Topic)
            .Include(assessment => assessment.Questions)
                .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(assessment => assessment.Id == assessmentId);

        if (assessment == null)
        {
            return NotFound($"Assessment with id {assessmentId} was not found.");
        }

        if (!assessment.IsPublished)
        {
            return BadRequest("Assessment is not published.");
        }

        var readinessIssues = _readinessService.GetReadinessIssues(assessment);

        if (readinessIssues.Count > 0)
        {
            return new FrontendSubmissionCreateResult
            {
                Status = FrontendSubmissionCreateStatus.NotReady,
                Error = "Assessment is not ready for frontend.",
                ReadinessIssues = readinessIssues
            };
        }

        var choiceQuestions = assessment.Questions
            .Where(IsFrontendChoiceQuestion)
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id)
            .ToList();

        // Open answers are required only for existing TextInput questions.
        // If the assessment has no TextInput questions, an empty openAnswers array is valid.

        var openQuestions = assessment.Questions
            .Where(IsFrontendOpenQuestion)
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id)
            .ToList();

        if (request.MultipleChoiceAnswers
            .GroupBy(answer => answer.QuestionId)
            .Any(group => group.Count() > 1))
        {
            return BadRequest("Multiple choice answers must not contain duplicate question ids.");
        }

        if (request.OpenAnswers
            .GroupBy(answer => answer.QuestionId)
            .Any(group => group.Count() > 1))
        {
            return BadRequest("Open answers must not contain duplicate question ids.");
        }

        var openQuestionIds = openQuestions
            .Select(question => question.Id)
            .ToList();

        var submittedOpenQuestionIds = request.OpenAnswers
            .Select(answer => answer.QuestionId)
            .ToList();

        var unknownOpenQuestionIds = submittedOpenQuestionIds
            .Where(questionId => !openQuestionIds.Contains(questionId))
            .ToList();

        if (unknownOpenQuestionIds.Count > 0)
        {
            return BadRequest("Open answers contain unknown question ids.");
        }

        var missingOpenQuestionIds = openQuestionIds
            .Where(questionId => !submittedOpenQuestionIds.Contains(questionId))
            .ToList();

        if (missingOpenQuestionIds.Count > 0)
        {
            return BadRequest("Open answers must contain answers for all open questions.");
        }

        var choiceQuestionIds = choiceQuestions
            .Select(question => question.Id)
            .ToList();

        var submittedChoiceQuestionIds = request.MultipleChoiceAnswers
            .Select(answer => answer.QuestionId)
            .ToList();

        var unknownChoiceQuestionIds = submittedChoiceQuestionIds
            .Where(questionId => !choiceQuestionIds.Contains(questionId))
            .ToList();

        if (unknownChoiceQuestionIds.Count > 0)
        {
            return BadRequest("Choice answers contain unknown question ids.");
        }

        var missingChoiceQuestionIds = choiceQuestionIds
            .Where(questionId => !submittedChoiceQuestionIds.Contains(questionId))
            .ToList();

        if (missingChoiceQuestionIds.Count > 0)
        {
            return BadRequest("Choice answers must contain answers for all choice questions.");
        }

        var choiceAnswersByQuestionId = request.MultipleChoiceAnswers
            .ToDictionary(answer => answer.QuestionId);

        var openAnswersByQuestionId = request.OpenAnswers
            .ToDictionary(answer => answer.QuestionId);

        // Topics are optional in the current MVP.
        // If the assessment has no topics, an empty selfEvaluation array is valid.

        var assessmentTopics = assessment.Topics
            .OrderBy(assessmentTopic => assessmentTopic.SortOrder)
            .ThenBy(assessmentTopic => assessmentTopic.Id)
            .ToList();

        var topicsById = assessmentTopics
            .ToDictionary(
                assessmentTopic => assessmentTopic.TopicId,
                assessmentTopic => assessmentTopic.Topic);

        if (request.SelfEvaluation.Any(selfEvaluation =>
            selfEvaluation.Rating < 1 ||
            selfEvaluation.Rating > 5))
        {
            return BadRequest("Self-evaluation ratings must be between 1 and 5.");
        }

        var submittedTopicIds = request.SelfEvaluation
            .Select(selfEvaluation => selfEvaluation.TopicId)
            .ToList();

        if (submittedTopicIds.Count != submittedTopicIds.Distinct().Count())
        {
            return BadRequest("Self-evaluation must not contain duplicate topic ids.");
        }

        var unknownTopicIds = submittedTopicIds
            .Where(topicId => !topicsById.ContainsKey(topicId))
            .ToList();

        if (unknownTopicIds.Count > 0)
        {
            return BadRequest("Self-evaluation contains unknown topic ids.");
        }

        var missingTopicIds = topicsById.Keys
            .Where(topicId => !submittedTopicIds.Contains(topicId))
            .ToList();

        if (missingTopicIds.Count > 0)
        {
            return BadRequest("Self-evaluation must contain ratings for all assessment topics.");
        }

        var submittedUtc = DateTime.UtcNow;

        var attempt = new AssessmentAttempt
        {
            AssessmentId = assessment.Id,
            ParticipantCode = participantCode,
            StartedUtc = submittedUtc,
            EndsUtc = assessment.TimeLimitMinutes == null
                ? null
                : submittedUtc.AddMinutes(assessment.TimeLimitMinutes.Value),
            SubmittedUtc = submittedUtc,
            SelfEvaluations = request.SelfEvaluation
                .OrderBy(selfEvaluation => selfEvaluation.TopicId)
                .Select(selfEvaluation => new AttemptSelfEvaluation
                {
                    TopicId = selfEvaluation.TopicId,
                    TopicName = topicsById[selfEvaluation.TopicId].Name,
                    Rating = selfEvaluation.Rating
                })
                .ToList()
        };

        foreach (var question in choiceQuestions)
        {
            var answerOptions = GetOrderedAnswerOptions(question);
            var correctIndex = GetCorrectIndex(answerOptions);

            if (!choiceAnswersByQuestionId.TryGetValue(question.Id, out var answer) ||
                answer.SelectedOption == null)
            {
                return BadRequest($"Question {question.Id} must have a selected option.");
            }

            if (answer.SelectedOption < 0 || answer.SelectedOption >= answerOptions.Count)
            {
                return BadRequest($"Selected option index is out of range for question {question.Id}.");
            }

            var selectedAnswerOption = answerOptions[answer.SelectedOption.Value];
            var isCorrect = answer.SelectedOption.Value == correctIndex;

            attempt.AttemptQuestions.Add(new AttemptQuestion
            {
                QuestionId = question.Id,
                IsCorrect = isCorrect,
                SelectedAnswerOptions = new List<AttemptAnswerOption>
                {
                    new AttemptAnswerOption
                    {
                        AnswerOptionId = selectedAnswerOption.Id
                    }
                }
            });
        }

        foreach (var question in openQuestions)
        {
            openAnswersByQuestionId.TryGetValue(question.Id, out var answer);

            attempt.AttemptQuestions.Add(new AttemptQuestion
            {
                QuestionId = question.Id,
                AnswerText = answer?.Text ?? string.Empty,
                IsCorrect = null
            });
        }

        _context.AssessmentAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        var savedAttemptId = attempt.Id;

        var savedAttempt = await _context.AssessmentAttempts
            .Include(attempt => attempt.SelfEvaluations)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.SelectedAnswerOptions)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.Question)
                    .ThenInclude(question => question.AnswerOptions)
            .FirstAsync(attempt => attempt.Id == savedAttemptId);

        var response = _submissionResponseService.BuildFrontendSubmissionResponse(savedAttempt);

        return new FrontendSubmissionCreateResult
        {
            Status = FrontendSubmissionCreateStatus.Created,
            AttemptId = savedAttemptId,
            Response = response
        };
    }

    // The current frontend quiz uses radio buttons and sends one selected option index, though it mentions "multiple".
    // It maps to SingleChoice in the backend.
    // Backend MultipleChoice can be supported later with checkbox UI and array payload.
    private static bool IsFrontendChoiceQuestion(Question question)
    {
        return question.QuestionType == QuestionType.SingleChoice;
    }

    private static bool IsFrontendOpenQuestion(Question question)
    {
        return question.QuestionType == QuestionType.TextInput;
    }

    private static List<AnswerOption> GetOrderedAnswerOptions(Question question)
    {
        return question.AnswerOptions
            .OrderBy(answerOption => answerOption.SortOrder)
            .ThenBy(answerOption => answerOption.Id)
            .ToList();
    }

    private static int GetCorrectIndex(List<AnswerOption> answerOptions)
    {
        return answerOptions.FindIndex(answerOption => answerOption.IsCorrect);
    }

    private static FrontendSubmissionCreateResult BadRequest(string error)
    {
        return new FrontendSubmissionCreateResult
        {
            Status = FrontendSubmissionCreateStatus.BadRequest,
            Error = error
        };
    }

    private static FrontendSubmissionCreateResult NotFound(string error)
    {
        return new FrontendSubmissionCreateResult
        {
            Status = FrontendSubmissionCreateStatus.NotFound,
            Error = error
        };
    }
}
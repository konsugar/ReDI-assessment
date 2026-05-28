using Microsoft.AspNetCore.Mvc;
using ReDI.Assessment.Api.Data;

using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.DTOs;

using ReDI.Assessment.Api.Models;

using ReDI.Assessment.Api.Services;

namespace ReDI.Assessment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly AssessmentDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly SubmissionResponseService _submissionResponseService;

    public SubmissionsController(
        AssessmentDbContext context,
        IConfiguration configuration,
        SubmissionResponseService submissionResponseService)
    {
        _context = context;
        _configuration = configuration;
        _submissionResponseService = submissionResponseService;
    }

    private int GetFrontendAssessmentId()
    {
        return _configuration.GetValue<int>("FrontendAssessment:AssessmentId");
    }

    private static bool IsFrontendChoiceQuestion(Question question)
    {
        // The current frontend quiz uses radio buttons and sends one selected option index, though it mentions "multiple".
        // It maps to SingleChoice in the backend.
        // Backend MultipleChoice can be supported later with checkbox UI and array payload.
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


    [HttpGet]
    public async Task<ActionResult<List<AdminSubmissionSummaryResponse>>> GetSubmissions()
    {
        var assessmentId = GetFrontendAssessmentId();

        var attempts = await _context.AssessmentAttempts
            .Include(attempt => attempt.SelfEvaluations)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.SelectedAnswerOptions)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.Question)
                    .ThenInclude(question => question.AnswerOptions)
            .Where(attempt =>
                attempt.AssessmentId == assessmentId &&
                attempt.SubmittedUtc != null)
            .OrderByDescending(attempt => attempt.SubmittedUtc)
            .ToListAsync();

        //var response = attempts
        //    .Select(attempt =>
        //    {
        //        var scoredQuestions = attempt.AttemptQuestions
        //            .Where(attemptQuestion => attemptQuestion.Question.QuestionType == QuestionType.SingleChoice)
        //            .ToList();

        //        var score = scoredQuestions
        //            .Count(attemptQuestion => attemptQuestion.IsCorrect == true);

        //        var total = scoredQuestions.Count;

        //        return new AdminSubmissionSummaryResponse
        //        {
        //            AttemptId = attempt.Id,
        //            SubmissionId = attempt.Id.ToString(),
        //            ParticipantCode = attempt.ParticipantCode,
        //            Timestamp = attempt.SubmittedUtc!.Value,
        //            Score = score,
        //            Total = total,
        //            Percentage = total == 0
        //                ? 0
        //                : (int)Math.Round((double)score / total * 100),
        //            SelfEvaluation = attempt.SelfEvaluations
        //                .OrderBy(selfEvaluation => selfEvaluation.TopicId)
        //                .Select(selfEvaluation => new FrontendSelfEvaluationResponse
        //                {
        //                    TopicId = selfEvaluation.TopicId,
        //                    TopicName = selfEvaluation.TopicName,
        //                    Rating = selfEvaluation.Rating
        //                })
        //                .ToList()
        //        };
        //    })
        //    .ToList();

        var response = attempts
            .Select(attempt => _submissionResponseService.BuildSummary(attempt))
            .ToList();

        return Ok(response);
    }


    [HttpGet("details")]
    public async Task<ActionResult<List<AdminSubmissionDetailsResponse>>> GetSubmissionDetails()
    {
        var assessmentId = GetFrontendAssessmentId();

        var attempts = await _context.AssessmentAttempts
            .Include(attempt => attempt.SelfEvaluations)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.SelectedAnswerOptions)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.Question)
                    .ThenInclude(question => question.AnswerOptions)
            .Where(attempt =>
                attempt.AssessmentId == assessmentId &&
                attempt.SubmittedUtc != null)
            .OrderByDescending(attempt => attempt.SubmittedUtc)
            .ToListAsync();

        var response = attempts
            .Select(attempt => _submissionResponseService.BuildDetails(attempt))
            .ToList();

        return Ok(response);
    }


    [HttpGet("{attemptId:int}")]
    public async Task<ActionResult<AdminSubmissionDetailsResponse>> GetSubmissionById(int attemptId)
    {
        var assessmentId = GetFrontendAssessmentId();

        var attempt = await _context.AssessmentAttempts
            .Include(attempt => attempt.SelfEvaluations)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.SelectedAnswerOptions)
            .Include(attempt => attempt.AttemptQuestions)
                .ThenInclude(attemptQuestion => attemptQuestion.Question)
                    .ThenInclude(question => question.AnswerOptions)
            .FirstOrDefaultAsync(attempt =>
                attempt.Id == attemptId &&
                attempt.AssessmentId == assessmentId &&
                attempt.SubmittedUtc != null);

        if (attempt == null)
        {
            return NotFound();
        }

        //var choiceAttemptQuestions = attempt.AttemptQuestions
        //    .Where(attemptQuestion => attemptQuestion.Question.QuestionType == QuestionType.SingleChoice)
        //    .OrderBy(attemptQuestion => attemptQuestion.Question.SortOrder)
        //    .ThenBy(attemptQuestion => attemptQuestion.QuestionId)
        //    .ToList();

        //var openAttemptQuestions = attempt.AttemptQuestions
        //    .Where(attemptQuestion => attemptQuestion.Question.QuestionType == QuestionType.TextInput)
        //    .OrderBy(attemptQuestion => attemptQuestion.Question.SortOrder)
        //    .ThenBy(attemptQuestion => attemptQuestion.QuestionId)
        //    .ToList();

        //var questionResults = choiceAttemptQuestions
        //    .Select(attemptQuestion =>
        //    {
        //        var answerOptions = GetOrderedAnswerOptions(attemptQuestion.Question);
        //        var selectedAnswerOptionId = attemptQuestion.SelectedAnswerOptions
        //            .Select(selectedAnswerOption => selectedAnswerOption.AnswerOptionId)
        //            .FirstOrDefault();

        //        var selectedOption = selectedAnswerOptionId == 0
        //            ? (int?)null
        //            : answerOptions.FindIndex(answerOption => answerOption.Id == selectedAnswerOptionId);

        //        if (selectedOption < 0)
        //        {
        //            selectedOption = null;
        //        }

        //        return new FrontendQuestionResultResponse
        //        {
        //            QuestionId = attemptQuestion.QuestionId,
        //            QuestionText = attemptQuestion.Question.Text,
        //            Options = answerOptions
        //                .Select(answerOption => answerOption.Text)
        //                .ToList(),
        //            SelectedOption = selectedOption,
        //            CorrectIndex = GetCorrectIndex(answerOptions),
        //            Correct = attemptQuestion.IsCorrect == true
        //        };
        //    })
        //    .ToList();

        //var score = questionResults.Count(result => result.Correct);
        //var total = questionResults.Count;
        //var percentage = total == 0
        //    ? 0
        //    : (int)Math.Round((double)score / total * 100);

        //var response = new AdminSubmissionDetailsResponse
        //{
        //    AttemptId = attempt.Id,
        //    SubmissionId = attempt.Id.ToString(),
        //    ParticipantCode = attempt.ParticipantCode,
        //    Timestamp = attempt.SubmittedUtc!.Value,
        //    Score = score,
        //    Total = total,
        //    Percentage = percentage,
        //    QuestionResults = questionResults,
        //    SelfEvaluation = attempt.SelfEvaluations
        //        .OrderBy(selfEvaluation => selfEvaluation.TopicId)
        //        .Select(selfEvaluation => new FrontendSelfEvaluationResponse
        //        {
        //            TopicId = selfEvaluation.TopicId,
        //            TopicName = selfEvaluation.TopicName,
        //            Rating = selfEvaluation.Rating
        //        })
        //        .ToList(),
        //    OpenAnswers = openAttemptQuestions
        //        .Select(attemptQuestion => new FrontendOpenAnswerResponse
        //        {
        //            QuestionId = attemptQuestion.QuestionId,
        //            QuestionText = attemptQuestion.Question.Text,
        //            Text = attemptQuestion.AnswerText ?? string.Empty
        //        })
        //        .ToList()
        //};

        var response = _submissionResponseService.BuildDetails(attempt);

        return Ok(response);
    }


    [HttpPost]
    public async Task<ActionResult<FrontendSubmissionResponse>> CreateSubmission(
    FrontendSubmissionRequest request)
    {
        var participantCode = request.ParticipantCode.Trim();

        if (string.IsNullOrWhiteSpace(participantCode))
        {
            return BadRequest("Participant code is required.");
        }
        
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

        var choiceQuestions = assessment.Questions
            .Where(IsFrontendChoiceQuestion)
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id)
            .ToList();

        var openQuestions = assessment.Questions
            .Where(IsFrontendOpenQuestion)
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id)
            .ToList();

        if (choiceQuestions.Count == 0 && openQuestions.Count == 0)
        {
            return BadRequest("Frontend assessment must contain at least one supported question.");
        }

        if (choiceQuestions.Any(question => GetOrderedAnswerOptions(question).Count == 0))
        {
            return BadRequest("All frontend choice questions must have answer options.");
        }

        //return BadRequest(
        //    $"Submission endpoint loaded {choiceQuestions.Count} choice questions and {openQuestions.Count} open questions. Saving is not implemented yet.");

        if (choiceQuestions.Any(question => GetOrderedAnswerOptions(question).Count(answerOption => answerOption.IsCorrect) != 1))
        {
            return BadRequest("All frontend choice questions must have exactly one correct answer option.");
        }

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

        var assessmentTopics = assessment.Topics
            .OrderBy(assessmentTopic => assessmentTopic.SortOrder)
            .ThenBy(assessmentTopic => assessmentTopic.Id)
            .ToList();

        var topicsById = assessmentTopics
            .ToDictionary(
                assessmentTopic => assessmentTopic.TopicId,
                assessmentTopic => assessmentTopic.Topic);

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

        //var questionResults = new List<FrontendQuestionResultResponse>();

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

            //questionResults.Add(new FrontendQuestionResultResponse
            //{
            //    QuestionId = question.Id,
            //    QuestionText = question.Text,
            //    Options = answerOptions
            //        .Select(answerOption => answerOption.Text)
            //        .ToList(),
            //    SelectedOption = answer.SelectedOption,
            //    CorrectIndex = correctIndex,
            //    Correct = isCorrect
            //});
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

        //var score = questionResults.Count(result => result.Correct);
        //var total = questionResults.Count;
        //var percentage = total == 0
        //    ? 0
        //    : (int)Math.Round((double)score / total * 100);

        //var response = new FrontendSubmissionResponse
        //{
        //    SubmissionId = attempt.Id.ToString(),
        //    ParticipantCode = attempt.ParticipantCode,
        //    Score = score,
        //    Total = total,
        //    Percentage = percentage,
        //    QuestionResults = questionResults,
        //    SelfEvaluation = request.SelfEvaluation
        //        .OrderBy(selfEvaluation => selfEvaluation.TopicId)
        //        .Select(selfEvaluation => new FrontendSelfEvaluationResponse
        //        {
        //            TopicId = selfEvaluation.TopicId,
        //            TopicName = topicsById[selfEvaluation.TopicId].Name,
        //            Rating = selfEvaluation.Rating
        //        })
        //        .ToList(),
        //    OpenAnswers = openQuestions
        //        .Select(question =>
        //        {
        //            openAnswersByQuestionId.TryGetValue(question.Id, out var answer);

        //            return new FrontendOpenAnswerResponse
        //            {
        //                QuestionId = question.Id,
        //                QuestionText = question.Text,
        //                Text = answer?.Text ?? string.Empty
        //            };
        //        })
        //        .ToList()
        //};

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

        return CreatedAtAction(
            nameof(AttemptsController.GetAttemptResult),
            "Attempts",
            new { attemptId = savedAttemptId },
            response);
    }
}
using ReDI.Assessment.Api.DTOs;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.Services;

public class SubmissionResponseService
{
    public AdminSubmissionSummaryResponse BuildSummary(AssessmentAttempt attempt)
    {
        var questionResults = BuildQuestionResults(attempt);
        var score = questionResults.Count(result => result.Correct);
        var total = questionResults.Count;

        return new AdminSubmissionSummaryResponse
        {
            AttemptId = attempt.Id,
            SubmissionId = attempt.Id.ToString(),
            ParticipantCode = attempt.ParticipantCode,
            Timestamp = attempt.SubmittedUtc!.Value,
            Score = score,
            Total = total,
            Percentage = CalculatePercentage(score, total),
            SelfEvaluation = BuildSelfEvaluation(attempt)
        };
    }

    public AdminSubmissionDetailsResponse BuildDetails(AssessmentAttempt attempt)
    {
        var questionResults = BuildQuestionResults(attempt);
        var score = questionResults.Count(result => result.Correct);
        var total = questionResults.Count;

        return new AdminSubmissionDetailsResponse
        {
            AttemptId = attempt.Id,
            SubmissionId = attempt.Id.ToString(),
            ParticipantCode = attempt.ParticipantCode,
            Timestamp = attempt.SubmittedUtc!.Value,
            Score = score,
            Total = total,
            Percentage = CalculatePercentage(score, total),
            QuestionResults = questionResults,
            SelfEvaluation = BuildSelfEvaluation(attempt),
            OpenAnswers = BuildOpenAnswers(attempt)
        };
    }


    public FrontendSubmissionResponse BuildFrontendSubmissionResponse(AssessmentAttempt attempt)
    {
        var questionResults = BuildQuestionResults(attempt);
        var score = questionResults.Count(result => result.Correct);
        var total = questionResults.Count;

        return new FrontendSubmissionResponse
        {
            SubmissionId = attempt.Id.ToString(),
            ParticipantCode = attempt.ParticipantCode,
            Score = score,
            Total = total,
            Percentage = CalculatePercentage(score, total),
            QuestionResults = questionResults,
            SelfEvaluation = BuildSelfEvaluation(attempt),
            OpenAnswers = BuildOpenAnswers(attempt)
        };
    }


    private static List<FrontendQuestionResultResponse> BuildQuestionResults(AssessmentAttempt attempt)
    {
        return attempt.AttemptQuestions
            .Where(attemptQuestion => attemptQuestion.Question.QuestionType == QuestionType.SingleChoice)
            .OrderBy(attemptQuestion => attemptQuestion.Question.SortOrder)
            .ThenBy(attemptQuestion => attemptQuestion.QuestionId)
            .Select(attemptQuestion =>
            {
                var answerOptions = GetOrderedAnswerOptions(attemptQuestion.Question);

                var selectedAnswerOptionId = attemptQuestion.SelectedAnswerOptions
                    .Select(selectedAnswerOption => selectedAnswerOption.AnswerOptionId)
                    .FirstOrDefault();

                var selectedOption = selectedAnswerOptionId == 0
                    ? (int?)null
                    : answerOptions.FindIndex(answerOption => answerOption.Id == selectedAnswerOptionId);

                if (selectedOption < 0)
                {
                    selectedOption = null;
                }

                return new FrontendQuestionResultResponse
                {
                    QuestionId = attemptQuestion.QuestionId,
                    QuestionText = attemptQuestion.Question.Text,
                    Options = answerOptions
                        .Select(answerOption => answerOption.Text)
                        .ToList(),
                    SelectedOption = selectedOption,
                    CorrectIndex = GetCorrectIndex(answerOptions),
                    Correct = attemptQuestion.IsCorrect == true
                };
            })
            .ToList();
    }

    private static List<FrontendSelfEvaluationResponse> BuildSelfEvaluation(AssessmentAttempt attempt)
    {
        return attempt.SelfEvaluations
            .OrderBy(selfEvaluation => selfEvaluation.TopicId)
            .Select(selfEvaluation => new FrontendSelfEvaluationResponse
            {
                TopicId = selfEvaluation.TopicId,
                TopicName = selfEvaluation.TopicName,
                Rating = selfEvaluation.Rating
            })
            .ToList();
    }

    private static List<FrontendOpenAnswerResponse> BuildOpenAnswers(AssessmentAttempt attempt)
    {
        return attempt.AttemptQuestions
            .Where(attemptQuestion => attemptQuestion.Question.QuestionType == QuestionType.TextInput)
            .OrderBy(attemptQuestion => attemptQuestion.Question.SortOrder)
            .ThenBy(attemptQuestion => attemptQuestion.QuestionId)
            .Select(attemptQuestion => new FrontendOpenAnswerResponse
            {
                QuestionId = attemptQuestion.QuestionId,
                QuestionText = attemptQuestion.Question.Text,
                Text = attemptQuestion.AnswerText ?? string.Empty
            })
            .ToList();
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

    private static int CalculatePercentage(int score, int total)
    {
        return total == 0
            ? 0
            : (int)Math.Round((double)score / total * 100);
    }
}
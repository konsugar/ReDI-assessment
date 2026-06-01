using ReDI.Assessment.Api.Models;
using AssessmentEntity = ReDI.Assessment.Api.Models.Assessment;

namespace ReDI.Assessment.Api.Services;

public class FrontendAssessmentReadinessService
{
    public List<string> GetReadinessIssues(AssessmentEntity assessment)
    {
        var issues = new List<string>();

        // TextInput questions are optional for MVP launch readiness.
        // An assessment can be launchable with SingleChoice questions only.
        // If there are no TextInput questions, the frontend should submit an empty openAnswers array.

        // Topics are optional for MVP launch readiness.
        // If an assessment has no topics, the frontend can submit an empty selfEvaluation array.
        // Backlog: decide whether every launchable assessment should require at least one topic.

        var choiceQuestions = assessment.Questions
            .Where(question => question.QuestionType == QuestionType.SingleChoice)
            .ToList();

        var openQuestions = assessment.Questions
            .Where(question => question.QuestionType == QuestionType.TextInput)
            .ToList();

        if (choiceQuestions.Count == 0 && openQuestions.Count == 0)
        {
            issues.Add("Assessment must contain at least one SingleChoice or TextInput question.");
        }

        foreach (var question in choiceQuestions)
        {
            var answerOptions = question.AnswerOptions
                .OrderBy(answerOption => answerOption.SortOrder)
                .ThenBy(answerOption => answerOption.Id)
                .ToList();

            if (answerOptions.Count == 0)
            {
                issues.Add($"Question {question.Id} has no answer options.");
            }

            var correctAnswerCount = answerOptions
                .Count(answerOption => answerOption.IsCorrect);

            if (correctAnswerCount != 1)
            {
                issues.Add($"Question {question.Id} must have exactly one correct answer option.");
            }
        }

        return issues;
    }
}
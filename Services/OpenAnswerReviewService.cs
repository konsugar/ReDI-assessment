using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.Models;

namespace ReDI.Assessment.Api.Services;

public class OpenAnswerReviewService
{
    private readonly AssessmentDbContext _context;

    public OpenAnswerReviewService(AssessmentDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string? Error)> EvaluateAsync(
        int attemptId,
        int attemptQuestionId,
        bool? isCorrect)
    {
        var attemptQuestion = await LoadAttemptQuestion(attemptId, attemptQuestionId);

        if (attemptQuestion == null)
        {
            return (false, "NotFound");
        }

        var validationError = ValidateOpenAnswer(attemptQuestion);

        if (validationError != null)
        {
            return (false, validationError);
        }

        attemptQuestion.IsCorrect = isCorrect;

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateTeacherNoteAsync(
        int attemptId,
        int attemptQuestionId,
        string teacherNote)
    {
        var attemptQuestion = await LoadAttemptQuestion(attemptId, attemptQuestionId);

        if (attemptQuestion == null)
        {
            return (false, "NotFound");
        }

        var validationError = ValidateOpenAnswer(attemptQuestion);

        if (validationError != null)
        {
            return (false, validationError);
        }

        attemptQuestion.TeacherNote = teacherNote.Trim();

        await _context.SaveChangesAsync();

        return (true, null);
    }

    private async Task<AttemptQuestion?> LoadAttemptQuestion(
        int attemptId,
        int attemptQuestionId)
    {
        return await _context.AttemptQuestions
            .Include(attemptQuestion => attemptQuestion.AssessmentAttempt)
            .Include(attemptQuestion => attemptQuestion.Question)
            .FirstOrDefaultAsync(attemptQuestion =>
                attemptQuestion.Id == attemptQuestionId &&
                attemptQuestion.AssessmentAttemptId == attemptId);
    }

    private static string? ValidateOpenAnswer(AttemptQuestion attemptQuestion)
    {
        if (attemptQuestion.AssessmentAttempt.SubmittedUtc == null)
        {
            return "This attempt has not been submitted yet.";
        }

        if (attemptQuestion.Question.QuestionType != QuestionType.TextInput)
        {
            return "Only TextInput answers can be reviewed manually.";
        }

        return null;
    }
}
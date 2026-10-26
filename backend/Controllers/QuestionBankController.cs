using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Data;
using ReDI.Assessment.Api.Models;
using AssessmentEntity = ReDI.Assessment.Api.Models.Assessment;

namespace ReDI.Assessment.Api.Controllers;

// The editor replaces a draft atomically. Published content and submitted answers
// remain immutable, and the revision prevents stale browser sessions overwriting edits.
[ApiController]
[Authorize]
[Route("api/assessments/{assessmentId:int}/question-bank")]
public class QuestionBankController(AssessmentDbContext db) : ControllerBase
{
    public record TopicItem(int Id, string Name, string? Description);
    public record ChoiceItem(int Id, string Text, List<string> Options, int CorrectIndex);
    public record OpenItem(int Id, string Text);
    public record BankRequest(string Version, List<TopicItem> Topics,
        List<ChoiceItem> Questions, List<OpenItem> OpenQuestions);

    private async Task<AssessmentEntity?> Load(int id) => await db.Assessments
        .Include(a => a.Topics).ThenInclude(t => t.Topic)
        .Include(a => a.Questions).ThenInclude(q => q.AnswerOptions)
        .Include(a => a.Attempts).AsSplitQuery().SingleOrDefaultAsync(a => a.Id == id);

    private static object Snapshot(AssessmentEntity a) => new
    {
        a.Id, a.Title, a.IsPublished, AttemptCount = a.Attempts.Count,
        Topics = a.Topics.OrderBy(t => t.SortOrder).ThenBy(t => t.Id)
            .Select(t => new TopicItem(t.TopicId, t.Topic.Name, t.Topic.Description)).ToList(),
        Questions = a.Questions.Where(q => q.QuestionType == QuestionType.SingleChoice)
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Id).Select(q =>
            {
                var options = q.AnswerOptions.OrderBy(o => o.SortOrder).ThenBy(o => o.Id).ToList();
                return new ChoiceItem(q.Id, q.Text, options.Select(o => o.Text).ToList(), options.FindIndex(o => o.IsCorrect));
            }).ToList(),
        OpenQuestions = a.Questions.Where(q => q.QuestionType == QuestionType.TextInput)
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Id).Select(q => new OpenItem(q.Id, q.Text)).ToList(),
        HasUnsupportedQuestions = a.Questions.Any(q => q.QuestionType != QuestionType.SingleChoice && q.QuestionType != QuestionType.TextInput)
    };

    private static string Version(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static object BankResponse(AssessmentEntity a)
    {
        var bank = Snapshot(a);
        return new { Version = Version(bank), Bank = bank };
    }

    [HttpGet]
    public async Task<IActionResult> Get(int assessmentId)
    {
        var a = await Load(assessmentId);
        return a == null ? NotFound() : Ok(BankResponse(a));
    }

    [HttpPut]
    public async Task<IActionResult> Save(int assessmentId, BankRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Version) || request.Topics == null || request.Questions == null || request.OpenQuestions == null)
            return BadRequest("A version and all three question-bank arrays are required.");
        if (request.Topics.Count > 100 || request.Questions.Count + request.OpenQuestions.Count > 500)
            return BadRequest("The question bank is too large (maximum 100 topics and 500 questions).");
        if (request.Topics.Any(t => t == null || string.IsNullOrWhiteSpace(t.Name) || t.Name.Trim().Length > 200 || (t.Description?.Length ?? 0) > 1000)
            || request.Topics.Select(t => t.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Topics.Count)
            return BadRequest("Topics require unique names up to 200 characters and descriptions up to 1000 characters.");
        if (request.Questions.Any(q => q == null || string.IsNullOrWhiteSpace(q.Text) || q.Text.Length > 1000 || q.Options == null
            || q.Options.Count < 2 || q.Options.Count > 20 || q.Options.Any(o => string.IsNullOrWhiteSpace(o) || o.Length > 500)
            || q.CorrectIndex < 0 || q.CorrectIndex >= q.Options.Count)
            || request.OpenQuestions.Any(q => q == null || string.IsNullOrWhiteSpace(q.Text) || q.Text.Length > 1000))
            return BadRequest("Questions require text up to 1000 characters. Choice questions require 2–20 nonempty options up to 500 characters and a valid correct answer.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var a = await Load(assessmentId);
        if (a == null) return NotFound();
        if (a.IsPublished || a.Attempts.Count != 0)
            return Conflict("This assessment is locked. Unpublish an unused assessment or clone it to edit a new draft.");
        if (a.Questions.Any(q => q.QuestionType != QuestionType.SingleChoice && q.QuestionType != QuestionType.TextInput))
            return Conflict("This assessment contains question types this editor does not support. No content was changed.");
        if (request.Version != Version(Snapshot(a)))
            return Conflict("Another session changed this assessment. Reload the question bank before saving again.");

        // Reuse global topics without silently altering topics used by another assessment.
        var topics = new List<Topic>();
        foreach (var item in request.Topics)
        {
            var name = item.Name.Trim();
            var description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
            var topic = await db.Topics.SingleOrDefaultAsync(t => t.Name == name);
            if (topic == null)
                topic = new Topic { Name = name, Description = description };
            else if ((topic.Description ?? "") != (description ?? ""))
            {
                if (await db.AssessmentTopics.AnyAsync(t => t.TopicId == topic.Id && t.AssessmentId != assessmentId))
                    return Conflict($"Topic '{name}' is shared with another assessment. Use a different name to give it a different description.");
                topic.Description = description;
            }
            topics.Add(topic);
        }

        db.Questions.RemoveRange(a.Questions);
        db.AssessmentTopics.RemoveRange(a.Topics);
        await db.SaveChangesAsync();
        a.Questions.Clear();
        a.Topics.Clear();
        a.Topics = topics.Select((t, i) => new AssessmentTopic { Topic = t, SortOrder = i }).ToList();
        a.Questions = request.Questions.Select((q, i) => new Question
        {
            Text = q.Text.Trim(), QuestionType = QuestionType.SingleChoice, SortOrder = i,
            AnswerOptions = q.Options.Select((o, n) => new AnswerOption { Text = o.Trim(), SortOrder = n, IsCorrect = n == q.CorrectIndex }).ToList()
        }).Concat(request.OpenQuestions.Select((q, i) => new Question
        {
            Text = q.Text.Trim(), QuestionType = QuestionType.TextInput, SortOrder = request.Questions.Count + i
        })).ToList();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(BankResponse(a));
    }
}

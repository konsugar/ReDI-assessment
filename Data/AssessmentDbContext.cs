using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Models;

using AssessmentEntity = ReDI.Assessment.Api.Models.Assessment;

namespace ReDI.Assessment.Api.Data;

public class AssessmentDbContext : DbContext
{
    public AssessmentDbContext(DbContextOptions<AssessmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<AssessmentEntity> Assessments { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<AnswerOption> AnswerOptions { get; set; }
    public DbSet<AssessmentAttempt> AssessmentAttempts { get; set; }
    public DbSet<AttemptQuestion> AttemptQuestions { get; set; }
    public DbSet<AttemptAnswerOption> AttemptAnswerOptions { get; set; }
    public DbSet<AttemptSelfEvaluation> AttemptSelfEvaluations { get; set; }
    public DbSet<AssessmentTopic> AssessmentTopics { get; set; }
    public DbSet<Topic> Topics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttemptQuestion>()
            .HasOne(aq => aq.Question)
            .WithMany()
            .HasForeignKey(aq => aq.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<AttemptAnswerOption>()
            .HasOne(aao => aao.AnswerOption)
            .WithMany()
            .HasForeignKey(aao => aao.AnswerOptionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Topic>()
            .HasIndex(topic => topic.Name)
            .IsUnique();

        modelBuilder.Entity<AssessmentTopic>()
            .HasIndex(assessmentTopic => new
            {
                assessmentTopic.AssessmentId,
                assessmentTopic.TopicId
            })
            .IsUnique();
    }

}
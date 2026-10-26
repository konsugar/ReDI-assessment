using Microsoft.EntityFrameworkCore;
using ReDI.Assessment.Api.Models;
using AssessmentEntity = ReDI.Assessment.Api.Models.Assessment;

namespace ReDI.Assessment.Api.Data;

public static class SeedData
{
    public const int DemoAssessmentId = 9001;
    public const int EditableDraftAssessmentId = 9002;

    private const int HtmlTopicId = 9001;
    private const int CssTopicId = 9002;
    private const int JavaScriptTopicId = 9003;
    private const int ResponsiveDesignTopicId = 9004;
    private const int VersionControlTopicId = 9005;

    public static void SeedDemoData(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssessmentEntity>().HasData(
            new AssessmentEntity
            {
                Id = DemoAssessmentId,
                Title = "Seed: Frontend Demo Assessment",
                Description = "Published assessment based on frontend default questions for student flow testing.",
                TimeLimitMinutes = 30,
                IsPublished = true,
                CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AssessmentEntity
            {
                Id = EditableDraftAssessmentId,
                Title = "Seed: Editable Draft Assessment",
                Description = "Small draft assessment for admin content editor testing.",
                TimeLimitMinutes = 30,
                IsPublished = false,
                CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<Topic>().HasData(
            new Topic
            {
                Id = HtmlTopicId,
                Name = "HTML",
                Description = "HyperText Markup Language"
            },
            new Topic
            {
                Id = CssTopicId,
                Name = "CSS",
                Description = "Cascading Style Sheets"
            },
            new Topic
            {
                Id = JavaScriptTopicId,
                Name = "JavaScript",
                Description = "Programming for the web"
            },
            new Topic
            {
                Id = ResponsiveDesignTopicId,
                Name = "Responsive Design",
                Description = "Mobile-first development"
            },
            new Topic
            {
                Id = VersionControlTopicId,
                Name = "Version Control",
                Description = "Code management with Git"
            }
        );

        modelBuilder.Entity<AssessmentTopic>().HasData(
            new AssessmentTopic
            {
                Id = 9001,
                AssessmentId = DemoAssessmentId,
                TopicId = HtmlTopicId,
                SortOrder = 0
            },
            new AssessmentTopic
            {
                Id = 9002,
                AssessmentId = DemoAssessmentId,
                TopicId = CssTopicId,
                SortOrder = 1
            },
            new AssessmentTopic
            {
                Id = 9003,
                AssessmentId = DemoAssessmentId,
                TopicId = JavaScriptTopicId,
                SortOrder = 2
            },
            new AssessmentTopic
            {
                Id = 9004,
                AssessmentId = DemoAssessmentId,
                TopicId = ResponsiveDesignTopicId,
                SortOrder = 3
            },
            new AssessmentTopic
            {
                Id = 9005,
                AssessmentId = DemoAssessmentId,
                TopicId = VersionControlTopicId,
                SortOrder = 4
            },
            new AssessmentTopic
            {
                Id = 9006,
                AssessmentId = EditableDraftAssessmentId,
                TopicId = HtmlTopicId,
                SortOrder = 0
            },
            new AssessmentTopic
            {
                Id = 9007,
                AssessmentId = EditableDraftAssessmentId,
                TopicId = CssTopicId,
                SortOrder = 1
            }
        );

        modelBuilder.Entity<Question>().HasData(
            new Question
            {
                Id = 9001,
                AssessmentId = DemoAssessmentId,
                Text = "What does HTML stand for?",
                QuestionType = QuestionType.SingleChoice,
                SortOrder = 0
            },
            new Question
            {
                Id = 9002,
                AssessmentId = DemoAssessmentId,
                Text = "Which CSS property changes the text color?",
                QuestionType = QuestionType.SingleChoice,
                SortOrder = 1
            },
            new Question
            {
                Id = 9003,
                AssessmentId = DemoAssessmentId,
                Text = "Which HTML tag is used to create a hyperlink?",
                QuestionType = QuestionType.SingleChoice,
                SortOrder = 2
            },
            new Question
            {
                Id = 9004,
                AssessmentId = DemoAssessmentId,
                Text = "Which CSS property is used to make text bold?",
                QuestionType = QuestionType.SingleChoice,
                SortOrder = 3
            },
            new Question
            {
                Id = 9005,
                AssessmentId = DemoAssessmentId,
                Text = "Which JavaScript method selects an element by its ID?",
                QuestionType = QuestionType.SingleChoice,
                SortOrder = 4
            },
            new Question
            {
                Id = 9006,
                AssessmentId = DemoAssessmentId,
                Text = "Which CSS property controls the space outside an element's border?",
                QuestionType = QuestionType.SingleChoice,
                SortOrder = 5
            },
            new Question
            {
                Id = 9007,
                AssessmentId = DemoAssessmentId,
                Text = "In HTML, which attribute provides alternative text for an image?",
                QuestionType = QuestionType.SingleChoice,
                SortOrder = 6
            },
            new Question
            {
                Id = 9008,
                AssessmentId = DemoAssessmentId,
                Text = "Which JavaScript keyword declares a variable that cannot be reassigned?",
                QuestionType = QuestionType.SingleChoice,
                SortOrder = 7
            },
            new Question
            {
                Id = 9009,
                AssessmentId = DemoAssessmentId,
                Text = "Describe the difference between margin and padding in CSS.",
                QuestionType = QuestionType.TextInput,
                SortOrder = 8
            },
            new Question
            {
                Id = 9010,
                AssessmentId = DemoAssessmentId,
                Text = "What is the purpose of the <head> element in HTML?",
                QuestionType = QuestionType.TextInput,
                SortOrder = 9
            },
            new Question
            {
                Id = 9011,
                AssessmentId = DemoAssessmentId,
                Text = "Explain what a function is in JavaScript and give a simple example.",
                QuestionType = QuestionType.TextInput,
                SortOrder = 10
            },
            new Question
            {
                Id = 9012,
                AssessmentId = EditableDraftAssessmentId,
                Text = "Draft question: which option is marked as correct?",
                QuestionType = QuestionType.SingleChoice,
                SortOrder = 0
            },
            new Question
            {
                Id = 9013,
                AssessmentId = EditableDraftAssessmentId,
                Text = "Draft open question for editor testing.",
                QuestionType = QuestionType.TextInput,
                SortOrder = 1
            }
        );

        modelBuilder.Entity<AnswerOption>().HasData(
            new AnswerOption
            {
                Id = 9001,
                QuestionId = 9001,
                Text = "Hyper Text Markup Language",
                IsCorrect = true,
                SortOrder = 0
            },
            new AnswerOption
            {
                Id = 9002,
                QuestionId = 9001,
                Text = "High Tech Modern Language",
                IsCorrect = false,
                SortOrder = 1
            },
            new AnswerOption
            {
                Id = 9003,
                QuestionId = 9001,
                Text = "Hyper Transfer Markup Language",
                IsCorrect = false,
                SortOrder = 2
            },
            new AnswerOption
            {
                Id = 9004,
                QuestionId = 9001,
                Text = "Home Tool Markup Language",
                IsCorrect = false,
                SortOrder = 3
            },
            new AnswerOption
            {
                Id = 9005,
                QuestionId = 9002,
                Text = "text-color",
                IsCorrect = false,
                SortOrder = 0
            },
            new AnswerOption
            {
                Id = 9006,
                QuestionId = 9002,
                Text = "font-color",
                IsCorrect = false,
                SortOrder = 1
            },
            new AnswerOption
            {
                Id = 9007,
                QuestionId = 9002,
                Text = "color",
                IsCorrect = true,
                SortOrder = 2
            },
            new AnswerOption
            {
                Id = 9008,
                QuestionId = 9002,
                Text = "text-style",
                IsCorrect = false,
                SortOrder = 3
            },
            new AnswerOption
            {
                Id = 9009,
                QuestionId = 9003,
                Text = "<link>",
                IsCorrect = false,
                SortOrder = 0
            },
            new AnswerOption
            {
                Id = 9010,
                QuestionId = 9003,
                Text = "<a>",
                IsCorrect = true,
                SortOrder = 1
            },
            new AnswerOption
            {
                Id = 9011,
                QuestionId = 9003,
                Text = "<href>",
                IsCorrect = false,
                SortOrder = 2
            },
            new AnswerOption
            {
                Id = 9012,
                QuestionId = 9003,
                Text = "<url>",
                IsCorrect = false,
                SortOrder = 3
            },
            new AnswerOption
            {
                Id = 9013,
                QuestionId = 9004,
                Text = "text-size: bold",
                IsCorrect = false,
                SortOrder = 0
            },
            new AnswerOption
            {
                Id = 9014,
                QuestionId = 9004,
                Text = "font-weight: bold",
                IsCorrect = true,
                SortOrder = 1
            },
            new AnswerOption
            {
                Id = 9015,
                QuestionId = 9004,
                Text = "text-weight: bold",
                IsCorrect = false,
                SortOrder = 2
            },
            new AnswerOption
            {
                Id = 9016,
                QuestionId = 9004,
                Text = "bold: true",
                IsCorrect = false,
                SortOrder = 3
            },
            new AnswerOption
            {
                Id = 9017,
                QuestionId = 9005,
                Text = "document.getElement()",
                IsCorrect = false,
                SortOrder = 0
            },
            new AnswerOption
            {
                Id = 9018,
                QuestionId = 9005,
                Text = "document.querySelector()",
                IsCorrect = false,
                SortOrder = 1
            },
            new AnswerOption
            {
                Id = 9019,
                QuestionId = 9005,
                Text = "document.getElementById()",
                IsCorrect = true,
                SortOrder = 2
            },
            new AnswerOption
            {
                Id = 9020,
                QuestionId = 9005,
                Text = "document.findById()",
                IsCorrect = false,
                SortOrder = 3
            },
            new AnswerOption
            {
                Id = 9021,
                QuestionId = 9006,
                Text = "padding",
                IsCorrect = false,
                SortOrder = 0
            },
            new AnswerOption
            {
                Id = 9022,
                QuestionId = 9006,
                Text = "margin",
                IsCorrect = true,
                SortOrder = 1
            },
            new AnswerOption
            {
                Id = 9023,
                QuestionId = 9006,
                Text = "border-spacing",
                IsCorrect = false,
                SortOrder = 2
            },
            new AnswerOption
            {
                Id = 9024,
                QuestionId = 9006,
                Text = "spacing",
                IsCorrect = false,
                SortOrder = 3
            },
            new AnswerOption
            {
                Id = 9025,
                QuestionId = 9007,
                Text = "title",
                IsCorrect = false,
                SortOrder = 0
            },
            new AnswerOption
            {
                Id = 9026,
                QuestionId = 9007,
                Text = "src",
                IsCorrect = false,
                SortOrder = 1
            },
            new AnswerOption
            {
                Id = 9027,
                QuestionId = 9007,
                Text = "alt",
                IsCorrect = true,
                SortOrder = 2
            },
            new AnswerOption
            {
                Id = 9028,
                QuestionId = 9007,
                Text = "href",
                IsCorrect = false,
                SortOrder = 3
            },
            new AnswerOption
            {
                Id = 9029,
                QuestionId = 9008,
                Text = "var",
                IsCorrect = false,
                SortOrder = 0
            },
            new AnswerOption
            {
                Id = 9030,
                QuestionId = 9008,
                Text = "let",
                IsCorrect = false,
                SortOrder = 1
            },
            new AnswerOption
            {
                Id = 9031,
                QuestionId = 9008,
                Text = "const",
                IsCorrect = true,
                SortOrder = 2
            },
            new AnswerOption
            {
                Id = 9032,
                QuestionId = 9008,
                Text = "static",
                IsCorrect = false,
                SortOrder = 3
            },
            new AnswerOption
            {
                Id = 9033,
                QuestionId = 9012,
                Text = "Correct draft option",
                IsCorrect = true,
                SortOrder = 0
            },
            new AnswerOption
            {
                Id = 9034,
                QuestionId = 9012,
                Text = "Incorrect draft option",
                IsCorrect = false,
                SortOrder = 1
            }
        );
    }
}
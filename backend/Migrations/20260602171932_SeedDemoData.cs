using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ReDI.Assessment.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Assessments",
                columns: new[] { "Id", "CreatedUtc", "Description", "IsPublished", "TimeLimitMinutes", "Title" },
                values: new object[,]
                {
                    { 9001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Published assessment based on frontend default questions for student flow testing.", true, 30, "Seed: Frontend Demo Assessment" },
                    { 9002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Small draft assessment for admin content editor testing.", false, 30, "Seed: Editable Draft Assessment" }
                });

            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 9001, "HyperText Markup Language", "HTML" },
                    { 9002, "Cascading Style Sheets", "CSS" },
                    { 9003, "Programming for the web", "JavaScript" },
                    { 9004, "Mobile-first development", "Responsive Design" },
                    { 9005, "Code management with Git", "Version Control" }
                });

            migrationBuilder.InsertData(
                table: "AssessmentTopics",
                columns: new[] { "Id", "AssessmentId", "SortOrder", "TopicId" },
                values: new object[,]
                {
                    { 9001, 9001, 0, 9001 },
                    { 9002, 9001, 1, 9002 },
                    { 9003, 9001, 2, 9003 },
                    { 9004, 9001, 3, 9004 },
                    { 9005, 9001, 4, 9005 },
                    { 9006, 9002, 0, 9001 },
                    { 9007, 9002, 1, 9002 }
                });

            migrationBuilder.InsertData(
                table: "Questions",
                columns: new[] { "Id", "AssessmentId", "QuestionType", "SortOrder", "Text" },
                values: new object[,]
                {
                    { 9001, 9001, 1, 0, "What does HTML stand for?" },
                    { 9002, 9001, 1, 1, "Which CSS property changes the text color?" },
                    { 9003, 9001, 1, 2, "Which HTML tag is used to create a hyperlink?" },
                    { 9004, 9001, 1, 3, "Which CSS property is used to make text bold?" },
                    { 9005, 9001, 1, 4, "Which JavaScript method selects an element by its ID?" },
                    { 9006, 9001, 1, 5, "Which CSS property controls the space outside an element's border?" },
                    { 9007, 9001, 1, 6, "In HTML, which attribute provides alternative text for an image?" },
                    { 9008, 9001, 1, 7, "Which JavaScript keyword declares a variable that cannot be reassigned?" },
                    { 9009, 9001, 5, 8, "Describe the difference between margin and padding in CSS." },
                    { 9010, 9001, 5, 9, "What is the purpose of the <head> element in HTML?" },
                    { 9011, 9001, 5, 10, "Explain what a function is in JavaScript and give a simple example." },
                    { 9012, 9002, 1, 0, "Draft question: which option is marked as correct?" },
                    { 9013, 9002, 5, 1, "Draft open question for editor testing." }
                });

            migrationBuilder.InsertData(
                table: "AnswerOptions",
                columns: new[] { "Id", "IsCorrect", "QuestionId", "SortOrder", "Text" },
                values: new object[,]
                {
                    { 9001, true, 9001, 0, "Hyper Text Markup Language" },
                    { 9002, false, 9001, 1, "High Tech Modern Language" },
                    { 9003, false, 9001, 2, "Hyper Transfer Markup Language" },
                    { 9004, false, 9001, 3, "Home Tool Markup Language" },
                    { 9005, false, 9002, 0, "text-color" },
                    { 9006, false, 9002, 1, "font-color" },
                    { 9007, true, 9002, 2, "color" },
                    { 9008, false, 9002, 3, "text-style" },
                    { 9009, false, 9003, 0, "<link>" },
                    { 9010, true, 9003, 1, "<a>" },
                    { 9011, false, 9003, 2, "<href>" },
                    { 9012, false, 9003, 3, "<url>" },
                    { 9013, false, 9004, 0, "text-size: bold" },
                    { 9014, true, 9004, 1, "font-weight: bold" },
                    { 9015, false, 9004, 2, "text-weight: bold" },
                    { 9016, false, 9004, 3, "bold: true" },
                    { 9017, false, 9005, 0, "document.getElement()" },
                    { 9018, false, 9005, 1, "document.querySelector()" },
                    { 9019, true, 9005, 2, "document.getElementById()" },
                    { 9020, false, 9005, 3, "document.findById()" },
                    { 9021, false, 9006, 0, "padding" },
                    { 9022, true, 9006, 1, "margin" },
                    { 9023, false, 9006, 2, "border-spacing" },
                    { 9024, false, 9006, 3, "spacing" },
                    { 9025, false, 9007, 0, "title" },
                    { 9026, false, 9007, 1, "src" },
                    { 9027, true, 9007, 2, "alt" },
                    { 9028, false, 9007, 3, "href" },
                    { 9029, false, 9008, 0, "var" },
                    { 9030, false, 9008, 1, "let" },
                    { 9031, true, 9008, 2, "const" },
                    { 9032, false, 9008, 3, "static" },
                    { 9033, true, 9012, 0, "Correct draft option" },
                    { 9034, false, 9012, 1, "Incorrect draft option" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9004);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9005);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9006);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9007);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9008);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9009);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9010);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9011);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9012);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9013);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9014);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9015);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9016);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9017);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9018);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9019);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9020);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9021);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9022);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9023);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9024);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9025);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9026);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9027);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9028);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9029);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9030);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9031);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9032);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9033);

            migrationBuilder.DeleteData(
                table: "AnswerOptions",
                keyColumn: "Id",
                keyValue: 9034);

            migrationBuilder.DeleteData(
                table: "AssessmentTopics",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "AssessmentTopics",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "AssessmentTopics",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "AssessmentTopics",
                keyColumn: "Id",
                keyValue: 9004);

            migrationBuilder.DeleteData(
                table: "AssessmentTopics",
                keyColumn: "Id",
                keyValue: 9005);

            migrationBuilder.DeleteData(
                table: "AssessmentTopics",
                keyColumn: "Id",
                keyValue: 9006);

            migrationBuilder.DeleteData(
                table: "AssessmentTopics",
                keyColumn: "Id",
                keyValue: 9007);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9009);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9010);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9011);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9013);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9004);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9005);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9006);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9007);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9008);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 9012);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 9004);

            migrationBuilder.DeleteData(
                table: "Topics",
                keyColumn: "Id",
                keyValue: 9005);

            migrationBuilder.DeleteData(
                table: "Assessments",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "Assessments",
                keyColumn: "Id",
                keyValue: 9002);
        }
    }
}

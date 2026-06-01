using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReDI.Assessment.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToAssessmentTopicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssessmentTopics_AssessmentId",
                table: "AssessmentTopics");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTopics_AssessmentId_TopicId",
                table: "AssessmentTopics",
                columns: new[] { "AssessmentId", "TopicId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssessmentTopics_AssessmentId_TopicId",
                table: "AssessmentTopics");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentTopics_AssessmentId",
                table: "AssessmentTopics",
                column: "AssessmentId");
        }
    }
}

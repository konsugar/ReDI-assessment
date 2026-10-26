using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReDI.Assessment.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertAssessmentTopicsToTopicLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentTopics_Topics_TopicId",
                table: "AssessmentTopics");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AssessmentTopics");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "AssessmentTopics");

            migrationBuilder.AlterColumn<int>(
                name: "TopicId",
                table: "AssessmentTopics",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentTopics_Topics_TopicId",
                table: "AssessmentTopics",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentTopics_Topics_TopicId",
                table: "AssessmentTopics");

            migrationBuilder.AlterColumn<int>(
                name: "TopicId",
                table: "AssessmentTopics",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AssessmentTopics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AssessmentTopics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentTopics_Topics_TopicId",
                table: "AssessmentTopics",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id");
        }
    }
}

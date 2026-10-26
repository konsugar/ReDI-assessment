using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReDI.Assessment.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantCodeToAssessmentAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParticipantCode",
                table: "AssessmentAttempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParticipantCode",
                table: "AssessmentAttempts");
        }
    }
}

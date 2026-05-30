using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReDI.Assessment.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherNoteToAttemptQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeacherNote",
                table: "AttemptQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeacherNote",
                table: "AttemptQuestions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReDI.Assessment.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptSelfEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttemptSelfEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentAttemptId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    TopicName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptSelfEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttemptSelfEvaluations_AssessmentAttempts_AssessmentAttemptId",
                        column: x => x.AssessmentAttemptId,
                        principalTable: "AssessmentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptSelfEvaluations_AssessmentAttemptId",
                table: "AttemptSelfEvaluations",
                column: "AssessmentAttemptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttemptSelfEvaluations");
        }
    }
}

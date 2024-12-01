using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewsAndQuestions8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Interviews_InterviewId1",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_InterviewId1",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "InterviewId1",
                table: "Questions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterviewId1",
                table: "Questions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_InterviewId1",
                table: "Questions",
                column: "InterviewId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Interviews_InterviewId1",
                table: "Questions",
                column: "InterviewId1",
                principalTable: "Interviews",
                principalColumn: "Id");
        }
    }
}

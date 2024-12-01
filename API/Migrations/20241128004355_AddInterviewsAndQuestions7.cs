using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewsAndQuestions7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Interviews_InterviewId1",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "InterviewId1",
                table: "Questions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Interviews_InterviewId1",
                table: "Questions",
                column: "InterviewId1",
                principalTable: "Interviews",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Interviews_InterviewId1",
                table: "Questions");

            migrationBuilder.AlterColumn<int>(
                name: "InterviewId1",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Interviews_InterviewId1",
                table: "Questions",
                column: "InterviewId1",
                principalTable: "Interviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

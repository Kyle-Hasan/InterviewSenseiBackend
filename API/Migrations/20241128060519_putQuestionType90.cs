using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class putQuestionType90 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interviews_AspNetUsers_CreatedById",
                table: "Interviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_AspNetUsers_CreatedById",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Interviews_InterviewId",
                table: "Questions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Questions",
                table: "Questions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interviews",
                table: "Interviews");

            migrationBuilder.RenameTable(
                name: "Questions",
                newName: "Question");

            migrationBuilder.RenameTable(
                name: "Interviews",
                newName: "Interview");

            migrationBuilder.RenameIndex(
                name: "IX_Questions_InterviewId",
                table: "Question",
                newName: "IX_Question_InterviewId");

            migrationBuilder.RenameIndex(
                name: "IX_Questions_CreatedById",
                table: "Question",
                newName: "IX_Question_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Interviews_CreatedById",
                table: "Interview",
                newName: "IX_Interview_CreatedById");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedDate",
                table: "Question",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Question",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "VideoLink",
                table: "Question",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedDate",
                table: "Interview",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Interview",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Question",
                table: "Question",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interview",
                table: "Interview",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Interview_AspNetUsers_CreatedById",
                table: "Interview",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Question_AspNetUsers_CreatedById",
                table: "Question",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Question_Interview_InterviewId",
                table: "Question",
                column: "InterviewId",
                principalTable: "Interview",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Interview_AspNetUsers_CreatedById",
                table: "Interview");

            migrationBuilder.DropForeignKey(
                name: "FK_Question_AspNetUsers_CreatedById",
                table: "Question");

            migrationBuilder.DropForeignKey(
                name: "FK_Question_Interview_InterviewId",
                table: "Question");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Question",
                table: "Question");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interview",
                table: "Interview");

            migrationBuilder.DropColumn(
                name: "VideoLink",
                table: "Question");

            migrationBuilder.RenameTable(
                name: "Question",
                newName: "Questions");

            migrationBuilder.RenameTable(
                name: "Interview",
                newName: "Interviews");

            migrationBuilder.RenameIndex(
                name: "IX_Question_InterviewId",
                table: "Questions",
                newName: "IX_Questions_InterviewId");

            migrationBuilder.RenameIndex(
                name: "IX_Question_CreatedById",
                table: "Questions",
                newName: "IX_Questions_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Interview_CreatedById",
                table: "Interviews",
                newName: "IX_Interviews_CreatedById");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedDate",
                table: "Questions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Questions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedDate",
                table: "Interviews",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Interviews",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Questions",
                table: "Questions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interviews",
                table: "Interviews",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Interviews_AspNetUsers_CreatedById",
                table: "Interviews",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_AspNetUsers_CreatedById",
                table: "Questions",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Interviews_InterviewId",
                table: "Questions",
                column: "InterviewId",
                principalTable: "Interviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

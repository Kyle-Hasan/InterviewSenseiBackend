using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveInterviews2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "secondsPerAnswer",
                table: "Interview",
                newName: "SecondsPerAnswer");

            migrationBuilder.RenameColumn(
                name: "isInteractive",
                table: "Interview",
                newName: "IsInteractive");

            migrationBuilder.AddColumn<string>(
                name: "VideoLink",
                table: "Interview",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoLink",
                table: "Interview");

            migrationBuilder.RenameColumn(
                name: "SecondsPerAnswer",
                table: "Interview",
                newName: "secondsPerAnswer");

            migrationBuilder.RenameColumn(
                name: "IsInteractive",
                table: "Interview",
                newName: "isInteractive");
        }
    }
}

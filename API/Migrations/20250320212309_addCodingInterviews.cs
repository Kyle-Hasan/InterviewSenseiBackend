using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class addCodingInterviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLive",
                table: "Interview");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Question",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "isPremade",
                table: "Question",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Message",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Message",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Interview",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isPremade",
                table: "Question");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Interview");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Question",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "IsLive",
                table: "Interview",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}

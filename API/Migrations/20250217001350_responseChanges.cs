using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class responseChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Feedback",
                table: "Response",
                newName: "PositiveFeedback");

            migrationBuilder.AddColumn<string>(
                name: "ExampleResponse",
                table: "Response",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NegativeFeedback",
                table: "Response",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExampleResponse",
                table: "Response");

            migrationBuilder.DropColumn(
                name: "NegativeFeedback",
                table: "Response");

            migrationBuilder.RenameColumn(
                name: "PositiveFeedback",
                table: "Response",
                newName: "Feedback");
        }
    }
}

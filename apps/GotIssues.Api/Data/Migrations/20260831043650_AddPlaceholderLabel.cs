using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GotIssues.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceholderLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "placeholder_records",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Label",
                table: "placeholder_records");
        }
    }
}

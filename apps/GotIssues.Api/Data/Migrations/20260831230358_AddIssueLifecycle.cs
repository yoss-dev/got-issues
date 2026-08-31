using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GotIssues.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssigneeSubject",
                table: "issues",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "issues",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "issues",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "issues",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Task");

            migrationBuilder.CreateIndex(
                name: "IX_issues_AssigneeSubject",
                table: "issues",
                column: "AssigneeSubject");

            migrationBuilder.AddForeignKey(
                name: "FK_issues_users_AssigneeSubject",
                table: "issues",
                column: "AssigneeSubject",
                principalTable: "users",
                principalColumn: "Subject",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_issues_users_AssigneeSubject",
                table: "issues");

            migrationBuilder.DropIndex(
                name: "IX_issues_AssigneeSubject",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "AssigneeSubject",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "issues");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "issues");
        }
    }
}

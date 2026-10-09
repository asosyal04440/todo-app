using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAtUtc",
                table: "TodoItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_DueAtUtc",
                table: "TodoItems",
                column: "DueAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TodoItems_DueAtUtc",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "ReminderSentAtUtc",
                table: "TodoItems");
        }
    }
}

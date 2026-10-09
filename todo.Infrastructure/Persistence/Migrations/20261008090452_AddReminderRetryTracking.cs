using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderRetryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReminderAttemptCount",
                table: "TodoItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderLastAttemptAtUtc",
                table: "TodoItems",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderAttemptCount",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "ReminderLastAttemptAtUtc",
                table: "TodoItems");
        }
    }
}

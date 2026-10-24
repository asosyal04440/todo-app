using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Infrastructure.Persistence.Migrations
{
    public partial class AddUniqueNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TodoLists_OwnerUserId",
                table: "TodoLists");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_TodoListId",
                table: "TodoItems");

            migrationBuilder.CreateIndex(
                name: "IX_TodoLists_OwnerUserId_Name",
                table: "TodoLists",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_TodoListId_Title",
                table: "TodoItems",
                columns: new[] { "TodoListId", "Title" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TodoLists_OwnerUserId_Name",
                table: "TodoLists");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_TodoListId_Title",
                table: "TodoItems");

            migrationBuilder.CreateIndex(
                name: "IX_TodoLists_OwnerUserId",
                table: "TodoLists",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_TodoListId",
                table: "TodoItems",
                column: "TodoListId");
        }
    }
}

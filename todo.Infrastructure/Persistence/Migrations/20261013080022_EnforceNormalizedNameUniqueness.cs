using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Infrastructure.Persistence.Migrations
{
    public partial class EnforceNormalizedNameUniqueness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [TodoLists] WHERE [NormalizedName] IS NULL)
                    OR EXISTS (SELECT 1 FROM [TodoItems] WHERE [NormalizedTitle] IS NULL)
                    THROW 51000, 'Run TodoDatabaseMigrator to backfill normalized names before enforcing uniqueness.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_TodoLists_OwnerUserId_Name",
                table: "TodoLists");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_TodoListId_Title",
                table: "TodoItems");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "TodoLists",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(240)",
                oldMaxLength: 240,
                oldNullable: true,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedTitle",
                table: "TodoItems",
                type: "nvarchar(360)",
                maxLength: 360,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(360)",
                oldMaxLength: 360,
                oldNullable: true,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.CreateIndex(
                name: "IX_TodoLists_OwnerUserId_NormalizedName",
                table: "TodoLists",
                columns: new[] { "OwnerUserId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_TodoListId_NormalizedTitle",
                table: "TodoItems",
                columns: new[] { "TodoListId", "NormalizedTitle" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TodoLists_OwnerUserId_NormalizedName",
                table: "TodoLists");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_TodoListId_NormalizedTitle",
                table: "TodoItems");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "TodoLists",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(240)",
                oldMaxLength: 240,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedTitle",
                table: "TodoItems",
                type: "nvarchar(360)",
                maxLength: 360,
                nullable: true,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(360)",
                oldMaxLength: 360,
                oldCollation: "Latin1_General_100_BIN2");

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
    }
}

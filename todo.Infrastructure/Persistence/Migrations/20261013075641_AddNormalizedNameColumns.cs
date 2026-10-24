using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TodoApi.Infrastructure.Persistence.Migrations
{
    public partial class AddNormalizedNameColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "TodoLists",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true,
                collation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedTitle",
                table: "TodoItems",
                type: "nvarchar(360)",
                maxLength: 360,
                nullable: true,
                collation: "Latin1_General_100_BIN2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "TodoLists");

            migrationBuilder.DropColumn(
                name: "NormalizedTitle",
                table: "TodoItems");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace MyBlog.Data.Migrations
{
    public partial class CorrectedSpellingUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlogUserId",
                table: "Comments");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlogUserId",
                table: "Comments",
                type: "text",
                nullable: true);
        }
    }
}

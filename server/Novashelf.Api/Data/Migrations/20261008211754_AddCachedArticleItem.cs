using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novashelf.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCachedArticleItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "item",
                table: "cached_articles",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "item",
                table: "cached_articles");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novashelf.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCachedArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cached_articles",
                columns: table => new
                {
                    content_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    html = table.Column<string>(type: "text", nullable: false),
                    byline = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    reading_minutes = table.Column<int>(type: "integer", nullable: false),
                    format_version = table.Column<int>(type: "integer", nullable: false),
                    extracted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cached_articles", x => x.content_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cached_articles");
        }
    }
}

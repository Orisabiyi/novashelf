using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novashelf.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_topics",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    followed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_topics", x => new { x.user_id, x.slug });
                    table.ForeignKey(
                        name: "FK_user_topics_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_topics");
        }
    }
}

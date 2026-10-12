using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novashelf.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lesson_progress",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    track = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    lesson = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lesson_progress", x => new { x.user_id, x.track, x.lesson });
                    table.ForeignKey(
                        name: "FK_lesson_progress_AspNetUsers_user_id",
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
                name: "lesson_progress");
        }
    }
}

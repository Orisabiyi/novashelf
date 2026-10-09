using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Novashelf.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reading_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    content_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    item = table.Column<string>(type: "jsonb", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reading_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_reading_history_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reading_history_user_id_content_id",
                table: "reading_history",
                columns: new[] { "user_id", "content_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reading_history_user_id_read_at",
                table: "reading_history",
                columns: new[] { "user_id", "read_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reading_history");
        }
    }
}

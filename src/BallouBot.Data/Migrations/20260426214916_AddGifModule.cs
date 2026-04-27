using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BallouBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGifModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GifConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Tenor"),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PreviewCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GifConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GifConfigs_GuildSettings_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildSettings",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GifConfigs_GuildId",
                table: "GifConfigs",
                column: "GuildId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GifConfigs");
        }
    }
}

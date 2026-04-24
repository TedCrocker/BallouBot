using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BallouBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRandomRichardModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RichardConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    MinIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 480),
                    MaxIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 480),
                    UseWhitelistMode = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RichardConfigs", x => x.Id);
                    table.UniqueConstraint("AK_RichardConfigs_GuildId", x => x.GuildId);
                    table.ForeignKey(
                        name: "FK_RichardConfigs_GuildSettings_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildSettings",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RichardUserEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ListType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RichardUserEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RichardUserEntries_RichardConfigs_GuildId",
                        column: x => x.GuildId,
                        principalTable: "RichardConfigs",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RichardConfigs_GuildId",
                table: "RichardConfigs",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RichardUserEntries_GuildId_UserId_ListType",
                table: "RichardUserEntries",
                columns: new[] { "GuildId", "UserId", "ListType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RichardUserEntries");

            migrationBuilder.DropTable(
                name: "RichardConfigs");
        }
    }
}

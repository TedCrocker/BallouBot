using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BallouBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "!"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.Id);
                    table.UniqueConstraint("AK_GuildSettings_GuildId", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "WelcomeConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false, defaultValue: "Welcome to {server}, {user}! You are member #{membercount}."),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UseEmbed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    EmbedColor = table.Column<string>(type: "TEXT", maxLength: 6, nullable: false, defaultValue: "5865F2"),
                    EmbedTitle = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WelcomeConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WelcomeConfigs_GuildSettings_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildSettings",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildSettings_GuildId",
                table: "GuildSettings",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WelcomeConfigs_GuildId",
                table: "WelcomeConfigs",
                column: "GuildId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WelcomeConfigs");

            migrationBuilder.DropTable(
                name: "GuildSettings");
        }
    }
}

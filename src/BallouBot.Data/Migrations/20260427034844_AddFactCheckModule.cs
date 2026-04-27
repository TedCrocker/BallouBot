using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BallouBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFactCheckModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FactCheckConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AiProvider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "OpenAI"),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "gpt-4o-mini"),
                    AzureEndpoint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CooldownSeconds = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 60),
                    MaxChecksPerHour = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 30),
                    MinMessageLength = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 20),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactCheckConfigs", x => x.Id);
                    table.UniqueConstraint("AK_FactCheckConfigs_GuildId", x => x.GuildId);
                    table.ForeignKey(
                        name: "FK_FactCheckConfigs_GuildSettings_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildSettings",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactCheckUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactCheckUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactCheckUsers_FactCheckConfigs_GuildId",
                        column: x => x.GuildId,
                        principalTable: "FactCheckConfigs",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FactCheckConfigs_GuildId",
                table: "FactCheckConfigs",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactCheckUsers_GuildId_UserId",
                table: "FactCheckUsers",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FactCheckUsers");

            migrationBuilder.DropTable(
                name: "FactCheckConfigs");
        }
    }
}

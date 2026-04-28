using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BallouBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorNotifyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorNotifySubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SubscribedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorNotifySubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorNotifySubscriptions_GuildId_UserId",
                table: "ErrorNotifySubscriptions",
                columns: new[] { "GuildId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorNotifySubscriptions");
        }
    }
}

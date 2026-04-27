using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BallouBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRichardFallbackChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "FallbackChannelId",
                table: "RichardConfigs",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FallbackChannelId",
                table: "RichardConfigs");
        }
    }
}

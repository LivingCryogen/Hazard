using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzProxy.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerNamesToGameSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlayerNames",
                table: "GameSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerNames",
                table: "GameSessions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzProxy.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Winner = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.GameId);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStats",
                columns: table => new
                {
                    InstallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    GamesStarted = table.Column<int>(type: "int", nullable: false),
                    GamesCompleted = table.Column<int>(type: "int", nullable: false),
                    GamesWon = table.Column<int>(type: "int", nullable: false),
                    FirstGameStarted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstGameCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastGameStarted = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastGameCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalGamesDuration = table.Column<TimeSpan>(type: "time", nullable: false),
                    AttacksWon = table.Column<int>(type: "int", nullable: false),
                    AttacksLost = table.Column<int>(type: "int", nullable: false),
                    AttacksTied = table.Column<int>(type: "int", nullable: false),
                    Conquests = table.Column<int>(type: "int", nullable: false),
                    Retreats = table.Column<int>(type: "int", nullable: false),
                    ForcedRetreats = table.Column<int>(type: "int", nullable: false),
                    AttackDiceRolled = table.Column<int>(type: "int", nullable: false),
                    DefenseDiceRolled = table.Column<int>(type: "int", nullable: false),
                    Moves = table.Column<int>(type: "int", nullable: false),
                    MaxAdvances = table.Column<int>(type: "int", nullable: false),
                    TradeIns = table.Column<int>(type: "int", nullable: false),
                    TotalOccupationBonus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStats", x => new { x.Name, x.InstallId });
                });

            migrationBuilder.CreateTable(
                name: "AttackActions",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstallID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    SourceTerritory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetTerritory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefenderName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttackerInitialArmies = table.Column<int>(type: "int", nullable: false),
                    DefenderInitialArmies = table.Column<int>(type: "int", nullable: false),
                    AttackerDice = table.Column<int>(type: "int", nullable: false),
                    DefenderDice = table.Column<int>(type: "int", nullable: false),
                    AttackerLoss = table.Column<int>(type: "int", nullable: false),
                    DefenderLoss = table.Column<int>(type: "int", nullable: false),
                    Retreated = table.Column<bool>(type: "bit", nullable: false),
                    Conquered = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttackActions", x => new { x.GameId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_AttackActions_GameSessions_GameId",
                        column: x => x.GameId,
                        principalTable: "GameSessions",
                        principalColumn: "GameId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttackActions_PlayerStats_DefenderName_InstallID",
                        columns: x => new { x.DefenderName, x.InstallID },
                        principalTable: "PlayerStats",
                        principalColumns: new[] { "Name", "InstallId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttackActions_PlayerStats_PlayerName_InstallID",
                        columns: x => new { x.PlayerName, x.InstallID },
                        principalTable: "PlayerStats",
                        principalColumns: new[] { "Name", "InstallId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MoveActions",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstallID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    SourceTerritory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetTerritory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxAdvanced = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoveActions", x => new { x.GameId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_MoveActions_GameSessions_GameId",
                        column: x => x.GameId,
                        principalTable: "GameSessions",
                        principalColumn: "GameId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MoveActions_PlayerStats_PlayerName_InstallID",
                        columns: x => new { x.PlayerName, x.InstallID },
                        principalTable: "PlayerStats",
                        principalColumns: new[] { "Name", "InstallId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradeActions",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstallID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false),
                    CardTargets = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TradeValue = table.Column<int>(type: "int", nullable: false),
                    OccupiedBonus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeActions", x => new { x.GameId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_TradeActions_GameSessions_GameId",
                        column: x => x.GameId,
                        principalTable: "GameSessions",
                        principalColumn: "GameId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeActions_PlayerStats_PlayerName_InstallID",
                        columns: x => new { x.PlayerName, x.InstallID },
                        principalTable: "PlayerStats",
                        principalColumns: new[] { "Name", "InstallId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttackActions_DefenderName_InstallID",
                table: "AttackActions",
                columns: new[] { "DefenderName", "InstallID" });

            migrationBuilder.CreateIndex(
                name: "IX_AttackActions_PlayerName_InstallID",
                table: "AttackActions",
                columns: new[] { "PlayerName", "InstallID" });

            migrationBuilder.CreateIndex(
                name: "IX_MoveActions_PlayerName_InstallID",
                table: "MoveActions",
                columns: new[] { "PlayerName", "InstallID" });

            migrationBuilder.CreateIndex(
                name: "IX_TradeActions_PlayerName_InstallID",
                table: "TradeActions",
                columns: new[] { "PlayerName", "InstallID" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttackActions");

            migrationBuilder.DropTable(
                name: "MoveActions");

            migrationBuilder.DropTable(
                name: "TradeActions");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "PlayerStats");
        }
    }
}

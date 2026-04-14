using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddRetroAchievementsGameIdToGameRoms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RetroAchievementsGameId",
                table: "GameRoms",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameRoms_RetroAchievementsGameId",
                table: "GameRoms",
                column: "RetroAchievementsGameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameRoms_RetroAchievementsGameId",
                table: "GameRoms");

            migrationBuilder.DropColumn(
                name: "RetroAchievementsGameId",
                table: "GameRoms");
        }
    }
}

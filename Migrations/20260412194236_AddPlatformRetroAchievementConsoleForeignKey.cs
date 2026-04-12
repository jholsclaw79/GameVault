using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformRetroAchievementConsoleForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RetroAchievementConsoleId",
                table: "Platforms",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_RetroAchievementConsoleId",
                table: "Platforms",
                column: "RetroAchievementConsoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Platforms_RetroAchievementConsoles_RetroAchievementConsoleId",
                table: "Platforms",
                column: "RetroAchievementConsoleId",
                principalTable: "RetroAchievementConsoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Platforms_RetroAchievementConsoles_RetroAchievementConsoleId",
                table: "Platforms");

            migrationBuilder.DropIndex(
                name: "IX_Platforms_RetroAchievementConsoleId",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "RetroAchievementConsoleId",
                table: "Platforms");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddGameRomSystemOwnershipAndCompletionFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "GameRoms",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPhysicallyOwned",
                table: "GameRoms",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE GameRoms rom
                INNER JOIN Games game ON game.IGDBId = rom.GameIGDBId
                SET
                    rom.IsCompleted = game.IsCompleted,
                    rom.IsPhysicallyOwned = game.IsPhysicallyOwned
                WHERE game.IsCompleted = 1 OR game.IsPhysicallyOwned = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "GameRoms");

            migrationBuilder.DropColumn(
                name: "IsPhysicallyOwned",
                table: "GameRoms");
        }
    }
}

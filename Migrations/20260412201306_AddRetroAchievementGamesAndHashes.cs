using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddRetroAchievementGamesAndHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetroAchievementGames",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RetroAchievementsGameId = table.Column<long>(type: "bigint", nullable: false),
                    RetroAchievementConsoleId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsoleName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageIcon = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AchievementsCount = table.Column<int>(type: "int", nullable: false),
                    LeaderboardsCount = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ForumTopicId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetroAchievementGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetroAchievementGames_RetroAchievementConsoles_RetroAchievem~",
                        column: x => x.RetroAchievementConsoleId,
                        principalTable: "RetroAchievementConsoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RetroAchievementGameHashes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RetroAchievementGameId = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetroAchievementGameHashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetroAchievementGameHashes_RetroAchievementGames_RetroAchiev~",
                        column: x => x.RetroAchievementGameId,
                        principalTable: "RetroAchievementGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RetroAchievementGameHashes_Hash",
                table: "RetroAchievementGameHashes",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_RetroAchievementGameHashes_RetroAchievementGameId_Hash",
                table: "RetroAchievementGameHashes",
                columns: new[] { "RetroAchievementGameId", "Hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetroAchievementGames_RetroAchievementConsoleId",
                table: "RetroAchievementGames",
                column: "RetroAchievementConsoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RetroAchievementGames_RetroAchievementsGameId",
                table: "RetroAchievementGames",
                column: "RetroAchievementsGameId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetroAchievementGameHashes");

            migrationBuilder.DropTable(
                name: "RetroAchievementGames");
        }
    }
}

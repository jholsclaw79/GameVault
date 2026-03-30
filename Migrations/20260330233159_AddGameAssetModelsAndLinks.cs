using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddGameAssetModelsAndLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DlcsIdsJson",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ExpandedGamesIdsJson",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ExpansionsIdsJson",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GenresIdsJson",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ScreenshotsIdsJson",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "VideosIdsJson",
                table: "Games");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Games_IGDBId",
                table: "Games",
                column: "IGDBId");

            migrationBuilder.CreateTable(
                name: "GameCovers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IGDBId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlphaChannel = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Animated = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Checksum = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Height = table.Column<int>(type: "int", nullable: true),
                    ImageId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Width = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameCovers", x => x.Id);
                    table.UniqueConstraint("AK_GameCovers_IGDBId", x => x.IGDBId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameDlcs",
                columns: table => new
                {
                    GameIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    DlcIGDBId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameDlcs", x => new { x.GameIGDBId, x.DlcIGDBId });
                    table.ForeignKey(
                        name: "FK_GameDlcs_Games_DlcIGDBId",
                        column: x => x.DlcIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameDlcs_Games_GameIGDBId",
                        column: x => x.GameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameExpandedGames",
                columns: table => new
                {
                    GameIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    ExpandedGameIGDBId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameExpandedGames", x => new { x.GameIGDBId, x.ExpandedGameIGDBId });
                    table.ForeignKey(
                        name: "FK_GameExpandedGames_Games_ExpandedGameIGDBId",
                        column: x => x.ExpandedGameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameExpandedGames_Games_GameIGDBId",
                        column: x => x.GameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameExpansions",
                columns: table => new
                {
                    GameIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    ExpansionIGDBId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameExpansions", x => new { x.GameIGDBId, x.ExpansionIGDBId });
                    table.ForeignKey(
                        name: "FK_GameExpansions_Games_ExpansionIGDBId",
                        column: x => x.ExpansionIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameExpansions_Games_GameIGDBId",
                        column: x => x.GameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameScreenshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IGDBId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlphaChannel = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Animated = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Checksum = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Height = table.Column<int>(type: "int", nullable: true),
                    ImageId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Width = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameScreenshots", x => x.Id);
                    table.UniqueConstraint("AK_GameScreenshots_IGDBId", x => x.IGDBId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameVideos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IGDBId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Checksum = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VideoId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVideos", x => x.Id);
                    table.UniqueConstraint("AK_GameVideos_IGDBId", x => x.IGDBId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IGDBId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Checksum = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                    table.UniqueConstraint("AK_Genres_IGDBId", x => x.IGDBId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameScreenshotLinks",
                columns: table => new
                {
                    GameIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    ScreenshotIGDBId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameScreenshotLinks", x => new { x.GameIGDBId, x.ScreenshotIGDBId });
                    table.ForeignKey(
                        name: "FK_GameScreenshotLinks_GameScreenshots_ScreenshotIGDBId",
                        column: x => x.ScreenshotIGDBId,
                        principalTable: "GameScreenshots",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameScreenshotLinks_Games_GameIGDBId",
                        column: x => x.GameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameVideoLinks",
                columns: table => new
                {
                    GameIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    VideoIGDBId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVideoLinks", x => new { x.GameIGDBId, x.VideoIGDBId });
                    table.ForeignKey(
                        name: "FK_GameVideoLinks_GameVideos_VideoIGDBId",
                        column: x => x.VideoIGDBId,
                        principalTable: "GameVideos",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameVideoLinks_Games_GameIGDBId",
                        column: x => x.GameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameGenres",
                columns: table => new
                {
                    GameIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    GenreIGDBId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameGenres", x => new { x.GameIGDBId, x.GenreIGDBId });
                    table.ForeignKey(
                        name: "FK_GameGenres_Games_GameIGDBId",
                        column: x => x.GameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameGenres_Genres_GenreIGDBId",
                        column: x => x.GenreIGDBId,
                        principalTable: "Genres",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GameCovers_IGDBId",
                table: "GameCovers",
                column: "IGDBId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameDlcs_DlcIGDBId",
                table: "GameDlcs",
                column: "DlcIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_GameExpandedGames_ExpandedGameIGDBId",
                table: "GameExpandedGames",
                column: "ExpandedGameIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_GameExpansions_ExpansionIGDBId",
                table: "GameExpansions",
                column: "ExpansionIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_GameGenres_GenreIGDBId",
                table: "GameGenres",
                column: "GenreIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_GameScreenshotLinks_ScreenshotIGDBId",
                table: "GameScreenshotLinks",
                column: "ScreenshotIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_GameScreenshots_IGDBId",
                table: "GameScreenshots",
                column: "IGDBId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameVideoLinks_VideoIGDBId",
                table: "GameVideoLinks",
                column: "VideoIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_GameVideos_IGDBId",
                table: "GameVideos",
                column: "IGDBId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genres_IGDBId",
                table: "Genres",
                column: "IGDBId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_GameCovers_CoverIGDBId",
                table: "Games",
                column: "CoverIGDBId",
                principalTable: "GameCovers",
                principalColumn: "IGDBId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_GameCovers_CoverIGDBId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "GameCovers");

            migrationBuilder.DropTable(
                name: "GameDlcs");

            migrationBuilder.DropTable(
                name: "GameExpandedGames");

            migrationBuilder.DropTable(
                name: "GameExpansions");

            migrationBuilder.DropTable(
                name: "GameGenres");

            migrationBuilder.DropTable(
                name: "GameScreenshotLinks");

            migrationBuilder.DropTable(
                name: "GameVideoLinks");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "GameScreenshots");

            migrationBuilder.DropTable(
                name: "GameVideos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Games_IGDBId",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "DlcsIdsJson",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExpandedGamesIdsJson",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExpansionsIdsJson",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GenresIdsJson",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotsIdsJson",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VideosIdsJson",
                table: "Games",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}

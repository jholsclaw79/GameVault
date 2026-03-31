using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddGameRomsAndLocalOnlyGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocalOnly",
                table: "Games",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GameRoms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlatformIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    GameIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FilePath = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Md5 = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sha1 = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameRoms_Games_GameIGDBId",
                        column: x => x.GameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameRoms_Platforms_PlatformIGDBId",
                        column: x => x.PlatformIGDBId,
                        principalTable: "Platforms",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GameRoms_GameIGDBId",
                table: "GameRoms",
                column: "GameIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRoms_Md5",
                table: "GameRoms",
                column: "Md5");

            migrationBuilder.CreateIndex(
                name: "IX_GameRoms_PlatformIGDBId_FilePath",
                table: "GameRoms",
                columns: new[] { "PlatformIGDBId", "FilePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameRoms_Sha1",
                table: "GameRoms",
                column: "Sha1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameRoms");

            migrationBuilder.DropColumn(
                name: "IsLocalOnly",
                table: "Games");
        }
    }
}

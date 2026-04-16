using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageSupportsForPerSystemSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Languages_IGDBId",
                table: "Languages",
                column: "IGDBId");

            migrationBuilder.CreateTable(
                name: "LanguageSupports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IGDBId = table.Column<long>(type: "bigint", nullable: false),
                    GameIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    LanguageIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    LanguageSupportTypeIGDBId = table.Column<long>(type: "bigint", nullable: true),
                    Checksum = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageSupports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LanguageSupports_Games_GameIGDBId",
                        column: x => x.GameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LanguageSupports_Languages_LanguageIGDBId",
                        column: x => x.LanguageIGDBId,
                        principalTable: "Languages",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageSupports_GameIGDBId",
                table: "LanguageSupports",
                column: "GameIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageSupports_IGDBId",
                table: "LanguageSupports",
                column: "IGDBId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LanguageSupports_LanguageIGDBId",
                table: "LanguageSupports",
                column: "LanguageIGDBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LanguageSupports");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Languages_IGDBId",
                table: "Languages");
        }
    }
}

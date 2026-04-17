using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddInvolvedCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvolvedCompanies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IGDBId = table.Column<long>(type: "bigint", nullable: false),
                    GameIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyIGDBId = table.Column<long>(type: "bigint", nullable: true),
                    Developer = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Publisher = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Porting = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Supporting = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Checksum = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvolvedCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvolvedCompanies_Games_GameIGDBId",
                        column: x => x.GameIGDBId,
                        principalTable: "Games",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InvolvedCompanies_GameIGDBId",
                table: "InvolvedCompanies",
                column: "GameIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_InvolvedCompanies_IGDBId",
                table: "InvolvedCompanies",
                column: "IGDBId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvolvedCompanies");
        }
    }
}

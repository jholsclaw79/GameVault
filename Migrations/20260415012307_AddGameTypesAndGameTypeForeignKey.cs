using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTypesAndGameTypeForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IGDBId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameTypes", x => x.Id);
                    table.UniqueConstraint("AK_GameTypes_IGDBId", x => x.IGDBId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GameTypes_IGDBId",
                table: "GameTypes",
                column: "IGDBId",
                unique: true);

            migrationBuilder.Sql("""
                UPDATE Games g
                LEFT JOIN GameTypes gt ON gt.IGDBId = g.GameTypeIGDBId
                SET g.GameTypeIGDBId = NULL
                WHERE g.GameTypeIGDBId IS NOT NULL
                  AND gt.IGDBId IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_GameTypes_GameTypeIGDBId",
                table: "Games",
                column: "GameTypeIGDBId",
                principalTable: "GameTypes",
                principalColumn: "IGDBId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_GameTypes_GameTypeIGDBId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "GameTypes");
        }
    }
}

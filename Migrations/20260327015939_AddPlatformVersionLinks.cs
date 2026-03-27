using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformVersionLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_PlatformVersions_IGDBId",
                table: "PlatformVersions",
                column: "IGDBId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Platforms_IGDBId",
                table: "Platforms",
                column: "IGDBId");

            migrationBuilder.CreateTable(
                name: "PlatformPlatformVersions",
                columns: table => new
                {
                    PlatformIGDBId = table.Column<long>(type: "bigint", nullable: false),
                    PlatformVersionIGDBId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformPlatformVersions", x => new { x.PlatformIGDBId, x.PlatformVersionIGDBId });
                    table.ForeignKey(
                        name: "FK_PlatformPlatformVersions_PlatformVersions_PlatformVersionIGD~",
                        column: x => x.PlatformVersionIGDBId,
                        principalTable: "PlatformVersions",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatformPlatformVersions_Platforms_PlatformIGDBId",
                        column: x => x.PlatformIGDBId,
                        principalTable: "Platforms",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPlatformVersions_PlatformVersionIGDBId",
                table: "PlatformPlatformVersions",
                column: "PlatformVersionIGDBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformPlatformVersions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PlatformVersions_IGDBId",
                table: "PlatformVersions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Platforms_IGDBId",
                table: "Platforms");
        }
    }
}

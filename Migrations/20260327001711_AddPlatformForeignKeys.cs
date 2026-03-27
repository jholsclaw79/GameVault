using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_PlatformTypes_IGDBId",
                table: "PlatformTypes",
                column: "IGDBId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PlatformLogos_IGDBId",
                table: "PlatformLogos",
                column: "IGDBId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PlatformFamilies_IGDBId",
                table: "PlatformFamilies",
                column: "IGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_PlatformFamilyIGDBId",
                table: "Platforms",
                column: "PlatformFamilyIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_PlatformLogoIGDBId",
                table: "Platforms",
                column: "PlatformLogoIGDBId");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_PlatformTypeIGDBId",
                table: "Platforms",
                column: "PlatformTypeIGDBId");

            migrationBuilder.AddForeignKey(
                name: "FK_Platforms_PlatformFamilies_PlatformFamilyIGDBId",
                table: "Platforms",
                column: "PlatformFamilyIGDBId",
                principalTable: "PlatformFamilies",
                principalColumn: "IGDBId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Platforms_PlatformLogos_PlatformLogoIGDBId",
                table: "Platforms",
                column: "PlatformLogoIGDBId",
                principalTable: "PlatformLogos",
                principalColumn: "IGDBId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Platforms_PlatformTypes_PlatformTypeIGDBId",
                table: "Platforms",
                column: "PlatformTypeIGDBId",
                principalTable: "PlatformTypes",
                principalColumn: "IGDBId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Platforms_PlatformFamilies_PlatformFamilyIGDBId",
                table: "Platforms");

            migrationBuilder.DropForeignKey(
                name: "FK_Platforms_PlatformLogos_PlatformLogoIGDBId",
                table: "Platforms");

            migrationBuilder.DropForeignKey(
                name: "FK_Platforms_PlatformTypes_PlatformTypeIGDBId",
                table: "Platforms");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PlatformTypes_IGDBId",
                table: "PlatformTypes");

            migrationBuilder.DropIndex(
                name: "IX_Platforms_PlatformFamilyIGDBId",
                table: "Platforms");

            migrationBuilder.DropIndex(
                name: "IX_Platforms_PlatformLogoIGDBId",
                table: "Platforms");

            migrationBuilder.DropIndex(
                name: "IX_Platforms_PlatformTypeIGDBId",
                table: "Platforms");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PlatformLogos_IGDBId",
                table: "PlatformLogos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PlatformFamilies_IGDBId",
                table: "PlatformFamilies");
        }
    }
}

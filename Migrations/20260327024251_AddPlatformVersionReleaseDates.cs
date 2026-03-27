using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformVersionReleaseDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformVersionReleaseDates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IGDBId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Checksum = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateFormatIGDBId = table.Column<long>(type: "bigint", nullable: true),
                    Human = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Month = table.Column<int>(type: "int", nullable: true),
                    PlatformVersionIGDBId = table.Column<long>(type: "bigint", nullable: true),
                    ReleaseRegionIGDBId = table.Column<long>(type: "bigint", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformVersionReleaseDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformVersionReleaseDates_PlatformVersions_PlatformVersion~",
                        column: x => x.PlatformVersionIGDBId,
                        principalTable: "PlatformVersions",
                        principalColumn: "IGDBId",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformVersionReleaseDates_IGDBId",
                table: "PlatformVersionReleaseDates",
                column: "IGDBId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformVersionReleaseDates_PlatformVersionIGDBId",
                table: "PlatformVersionReleaseDates",
                column: "PlatformVersionIGDBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformVersionReleaseDates");
        }
    }
}

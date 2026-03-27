using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPlatformFieldCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "PlatformTypes",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Abbreviation",
                table: "Platforms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AlternativeName",
                table: "Platforms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "Platforms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Generation",
                table: "Platforms",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PlatformFamilyIGDBId",
                table: "Platforms",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PlatformLogoIGDBId",
                table: "Platforms",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PlatformTypeIGDBId",
                table: "Platforms",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Platforms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Platforms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Platforms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VersionsIdsJson",
                table: "Platforms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WebsitesIdsJson",
                table: "Platforms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "AlphaChannel",
                table: "PlatformLogos",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Animated",
                table: "PlatformLogos",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "PlatformLogos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "PlatformLogos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "PlatformLogos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "PlatformFamilies",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "PlatformFamilies",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "PlatformTypes");

            migrationBuilder.DropColumn(
                name: "Abbreviation",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "AlternativeName",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "Generation",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "PlatformFamilyIGDBId",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "PlatformLogoIGDBId",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "PlatformTypeIGDBId",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "VersionsIdsJson",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "WebsitesIdsJson",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "AlphaChannel",
                table: "PlatformLogos");

            migrationBuilder.DropColumn(
                name: "Animated",
                table: "PlatformLogos");

            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "PlatformLogos");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "PlatformLogos");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "PlatformLogos");

            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "PlatformFamilies");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "PlatformFamilies");
        }
    }
}

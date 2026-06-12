using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChordFlow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "Progressions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subgenre",
                table: "Progressions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Progressions",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Progressions");

            migrationBuilder.DropColumn(
                name: "Subgenre",
                table: "Progressions");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Progressions");
        }
    }
}

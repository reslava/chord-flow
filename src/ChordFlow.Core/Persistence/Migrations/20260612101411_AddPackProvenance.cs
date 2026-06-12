using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChordFlow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPackProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PackId",
                table: "Progressions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackId",
                table: "Progressions");
        }
    }
}

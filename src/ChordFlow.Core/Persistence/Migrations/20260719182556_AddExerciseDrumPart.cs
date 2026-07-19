using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChordFlow.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseDrumPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DrumGrooveId",
                table: "Exercises",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DrumMuted",
                table: "Exercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "DrumVolume",
                table: "Exercises",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DrumGrooveId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "DrumMuted",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "DrumVolume",
                table: "Exercises");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChordFlow.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorExerciseToSongRefs : Migration
    {
        // Clean drop-and-add rather than EF's positional column renames (which would map RhythmId→SongId and
        // ProgressionId→Feel — garbage). No data preservation needed (no users — IN4); seed exercises are
        // re-saved against Songs.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Key", table: "Exercises");
            migrationBuilder.DropColumn(name: "ProgressionId", table: "Exercises");
            migrationBuilder.DropColumn(name: "RhythmId", table: "Exercises");

            migrationBuilder.AddColumn<string>(
                name: "SongId", table: "Exercises", type: "TEXT", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "CompingPatternId", table: "Exercises", type: "TEXT", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "LeadPatternId", table: "Exercises", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "KeyOverride", table: "Exercises", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "Feel", table: "Exercises", type: "TEXT", nullable: false, defaultValue: "Straight");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SongId", table: "Exercises");
            migrationBuilder.DropColumn(name: "CompingPatternId", table: "Exercises");
            migrationBuilder.DropColumn(name: "LeadPatternId", table: "Exercises");
            migrationBuilder.DropColumn(name: "KeyOverride", table: "Exercises");
            migrationBuilder.DropColumn(name: "Feel", table: "Exercises");

            migrationBuilder.AddColumn<int>(
                name: "Key", table: "Exercises", type: "INTEGER", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(
                name: "ProgressionId", table: "Exercises", type: "TEXT", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "RhythmId", table: "Exercises", type: "TEXT", nullable: false, defaultValue: "");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChordFlow.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContentCompositeKeyIdOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Voicings",
                table: "Voicings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Songs",
                table: "Songs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RhythmPatterns",
                table: "RhythmPatterns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Progressions",
                table: "Progressions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Voicings",
                table: "Voicings",
                columns: new[] { "Id", "Origin" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Songs",
                table: "Songs",
                columns: new[] { "Id", "Origin" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_RhythmPatterns",
                table: "RhythmPatterns",
                columns: new[] { "Id", "Origin" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Progressions",
                table: "Progressions",
                columns: new[] { "Id", "Origin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Voicings",
                table: "Voicings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Songs",
                table: "Songs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RhythmPatterns",
                table: "RhythmPatterns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Progressions",
                table: "Progressions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Voicings",
                table: "Voicings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Songs",
                table: "Songs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RhythmPatterns",
                table: "RhythmPatterns",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Progressions",
                table: "Progressions",
                column: "Id");
        }
    }
}

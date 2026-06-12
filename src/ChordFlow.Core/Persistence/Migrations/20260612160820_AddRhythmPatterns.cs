using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChordFlow.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRhythmPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RhythmPatterns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Dsl = table.Column<string>(type: "TEXT", nullable: false),
                    TsNumerator = table.Column<int>(type: "INTEGER", nullable: false),
                    TsDenominator = table.Column<int>(type: "INTEGER", nullable: false),
                    Origin = table.Column<string>(type: "TEXT", nullable: false),
                    PackId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RhythmPatterns", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RhythmPatterns");
        }
    }
}

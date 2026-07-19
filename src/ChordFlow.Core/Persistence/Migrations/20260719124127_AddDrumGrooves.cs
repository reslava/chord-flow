using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChordFlow.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDrumGrooves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DrumGrooves",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Origin = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Dsl = table.Column<string>(type: "TEXT", nullable: false),
                    TsNumerator = table.Column<int>(type: "INTEGER", nullable: false),
                    TsDenominator = table.Column<int>(type: "INTEGER", nullable: false),
                    PackId = table.Column<string>(type: "TEXT", nullable: true),
                    Genre = table.Column<string>(type: "TEXT", nullable: true),
                    Subgenre = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrumGrooves", x => new { x.Id, x.Origin });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrumGrooves");
        }
    }
}

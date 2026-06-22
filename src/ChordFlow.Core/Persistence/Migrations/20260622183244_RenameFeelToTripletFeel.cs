using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChordFlow.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameFeelToTripletFeel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Feel",
                table: "Exercises",
                newName: "TripletFeel");

            // Remap legacy by-name values to the alphaTab TripletFeel vocabulary (best-effort: the old
            // Swing/Shuffle/Triplet all collapse onto Triplet8th, the one swung feel wired today).
            migrationBuilder.Sql("UPDATE Exercises SET TripletFeel = 'None' WHERE TripletFeel = 'Straight';");
            migrationBuilder.Sql("UPDATE Exercises SET TripletFeel = 'Triplet8th' WHERE TripletFeel IN ('Swing', 'Shuffle', 'Triplet');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Exercises SET TripletFeel = 'Straight' WHERE TripletFeel = 'None';");
            migrationBuilder.Sql("UPDATE Exercises SET TripletFeel = 'Swing' WHERE TripletFeel IN ('Triplet8th', 'Triplet16th');");

            migrationBuilder.RenameColumn(
                name: "TripletFeel",
                table: "Exercises",
                newName: "Feel");
        }
    }
}

using ChordFlow.Bridge;
using ChordFlow.Features.Voicings;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The GuitarVoicingsR oracle cross-check (req IN9): the <c>voicingGrid</c> handler is the engine's visual oracle,
/// so a representative filtered grid must reproduce the authored golden grips. Here: the <b>dom7 shells</b> — the
/// filter {family=shell, 3rd=major, 5th=perfect, 7th=7} isolates exactly Dominant7 — checked against the authored
/// shell oracle at C (<see cref="ShellOracleTests"/>) and against the deriver across all 12 roots, proving the grid
/// is a faithful projection that doesn't distort the realized grips. Pins the rules-reference shell shape too
/// (root + 3rd + 7th, the 5th omitted).
/// </summary>
public class VoicingGridOracleTests
{
    private static readonly VoicingGridHandler Handler = new();

    private static VoicingGridFilter Dom7Shells(int root) =>
        new(root, Sources: ["automatic"], Families: ["shell"], Thirds: ["major"], Fifths: ["perfect"], Sevenths: ["7"]);

    // Diagram → "s6 s5 s4 s3 s2 s1" fret string (muted = "x"), the same form the authored shell oracle uses.
    private static string FretString(FretboardDiagram diagram)
    {
        Dictionary<int, int> byString = diagram.Markers.ToDictionary(m => m.String, m => m.Fret);
        return string.Join(" ", Enumerable.Range(1, 6).Reverse()
            .Select(s => byString.TryGetValue(s, out int fret) ? fret.ToString() : "x"));
    }

    [Fact]
    public void Grid_Dom7Shells_AtCRoot_MatchTheAuthoredOracle()
    {
        var cellsById = Handler.Build(Dom7Shells(root: 0)).Cells.ToDictionary(c => c.Id);

        // Exactly the two shell forms, each matching the hand-authored grip (the spec ShellDerivation reproduces).
        Assert.Equal(2, cellsById.Count);
        Assert.Equal("x 3 2 3 x x", FretString(cellsById["auto:shell:dom7:C"].Diagram));
        Assert.Equal("8 x 8 9 x x", FretString(cellsById["auto:shell:dom7:E"].Diagram));
    }

    [Fact]
    public void Grid_Dom7Shells_AcrossAllTwelveRoots_MatchTheDeriver()
    {
        foreach (int root in Enumerable.Range(0, 12))
        {
            IReadOnlyList<VoicingGridCell> cells = Handler.Build(Dom7Shells(root)).Cells;

            Assert.All(cells, cell =>
            {
                Assert.Equal("Dominant7", cell.Quality);
                CagedShape form = Enum.Parse<CagedShape>(cell.Shape);
                string expected = ShellDerivation.Derive(Quality.Dominant7, form, new PitchClass(root), 0, 15).FretString();
                Assert.Equal(expected, FretString(cell.Diagram));
            });
            // Both 2-form shells realize at every root (a compact shell is always voiceable).
            Assert.Equal(2, cells.Count);
        }
    }

    // The rules-reference shell shape, asserted at the grid level: a shell sounds exactly root + 3rd + 7th, 5th omitted.
    [Fact]
    public void Grid_Dom7ShellCells_SoundRootThirdSeventh_NoFifth()
    {
        IReadOnlyList<VoicingGridCell> cells = Handler.Build(Dom7Shells(root: 0)).Cells;

        Assert.All(cells, cell =>
        {
            var functions = cell.Diagram.Markers.Select(m => m.Function).OrderBy(f => f).ToArray();
            Assert.Equal(new[] { "root", "seventh", "third" }, functions);
        });
    }
}

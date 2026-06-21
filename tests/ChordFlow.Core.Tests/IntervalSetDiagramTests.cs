using ChordFlow.Music.Harmony;
using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The scale / interval-set producer: pins that an interval set rooted at a pitch class lights every degree at
/// every fretboard position, keeps the user's typed token as each marker's label, leaves the window open for
/// auto-fit, and has no muted strings (many-per-string, unlike a voicing). Geometry comes from
/// <see cref="IntervalLattice"/>; this only checks the projection into <see cref="FretboardDiagram"/>.
/// </summary>
public class IntervalSetDiagramTests
{
    private static readonly PitchClass A = new(9);
    private static readonly PitchClass C = new(0);

    [Fact]
    public void Build_MinorPentatonicOnA_LightsOnlyTheRequestedDegrees()
    {
        FretboardDiagram diagram = IntervalSetDiagram.Build("1 b3 4 5 b7", A);

        Assert.NotEmpty(diagram.Markers);
        Assert.All(diagram.Markers, m => Assert.Contains(m.Interval, new[] { "1", "b3", "4", "5", "b7" }));
        // The low-E A (string 6, fret 5) is a root of the set.
        Assert.Contains(diagram.Markers, m => m is { String: 6, Fret: 5, Interval: "1", Note: "A" });
    }

    [Fact]
    public void Build_LeavesTheWindowOpenAndHasNoMutedStrings()
    {
        FretboardDiagram diagram = IntervalSetDiagram.Build("1 b3 5", A);

        Assert.Null(diagram.FretMin);
        Assert.Null(diagram.FretMax);
        Assert.Empty(diagram.MutedStrings);
        Assert.Null(diagram.BarreFret);
    }

    [Fact]
    public void Build_PreservesTheUsersSharpSpelling()
    {
        // A typed "#4" must read "#4", not its enharmonic flat "b5", even though both place at pitch class 6.
        FretboardDiagram diagram = IntervalSetDiagram.Build("1 #4", C);

        Assert.Contains(diagram.Markers, m => m is { Interval: "#4", String: 6, Fret: 2 }); // low-E F# = #4 of C
        Assert.DoesNotContain(diagram.Markers, m => m.Interval == "b5");
    }

    [Fact]
    public void Build_EmptyInput_ProducesAnEmptyDiagramTitledByTheRoot()
    {
        FretboardDiagram diagram = IntervalSetDiagram.Build("   ", C);

        Assert.Empty(diagram.Markers);
        Assert.Equal("C", diagram.Title);
    }

    [Fact]
    public void Build_TitleNamesTheRootAndTheSet()
    {
        FretboardDiagram diagram = IntervalSetDiagram.Build("1 b3 4 5 b7", A);
        Assert.Equal("A — 1 b3 4 5 b7", diagram.Title);
    }
}

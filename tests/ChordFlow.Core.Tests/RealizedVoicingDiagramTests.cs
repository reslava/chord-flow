using ChordFlow.Music.Harmony;
using Xunit;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The real-root voicing producer (IN2): turns a concrete <see cref="Voicing"/> at a chord's actual root into a
/// <see cref="FretboardDiagram"/>, intervals/functions resolved against <see cref="Chord.Root"/> (not canonical-C).
/// Covers a dominant-7 mapping (incl. the b7 label) and a non-C root with muted-string chrome.
/// </summary>
public class RealizedVoicingDiagramTests
{
    private static FretboardMarker Marker(FretboardDiagram d, int stringNumber) =>
        d.Markers.Single(m => m.String == stringNumber);

    [Fact]
    public void OpenG7_MapsEveryStringToItsIntervalAndFunctionAtTheRealRoot()
    {
        var chord = new Chord(new PitchClass(7), Quality.Dominant7); // G7 = G B D F
        var key = new Key(new PitchClass(7), IsMinor: false);        // G major
        // Open G7: low-E 3, A 2, D 0, G 0, B 0, high-E 1.
        var voicing = new Voicing(
            new[]
            {
                new FretPosition(6, 3), new FretPosition(5, 2), new FretPosition(4, 0),
                new FretPosition(3, 0), new FretPosition(2, 0), new FretPosition(1, 1),
            },
            FirstFret: 0);

        FretboardDiagram d = RealizedVoicingDiagram.Build(chord, voicing, key);

        Assert.Equal("G7", d.Title);       // spelled at the real root, against the key
        Assert.Equal(0, d.FretMin);
        Assert.Equal(6, d.Markers.Count);  // all six strings sound
        Assert.Empty(d.MutedStrings);
        Assert.All(d.Markers, m => Assert.Equal(MarkerShape.Circle, m.Shape));

        FretboardMarker low = Marker(d, 6); // low E, 3rd fret → G, the root
        Assert.Equal("G", low.Note);
        Assert.Equal("R", low.Interval);
        Assert.Equal("root", low.Function);

        Assert.Equal("third", Marker(d, 5).Function); // A string, 2nd fret → B
        Assert.Equal("3", Marker(d, 5).Interval);
        Assert.Equal("fifth", Marker(d, 4).Function); // D string, open → D
        Assert.Equal("5", Marker(d, 4).Interval);

        FretboardMarker high = Marker(d, 1); // high E, 1st fret → F, the dominant 7th
        Assert.Equal("F", high.Note);
        Assert.Equal("b7", high.Interval);
        Assert.Equal("seventh", high.Function);
    }

    [Fact]
    public void OpenDMajor_AnchorsIntervalsAtDAndMutesTheLowStrings()
    {
        var chord = new Chord(new PitchClass(2), Quality.Major); // D = D F# A
        var key = new Key(new PitchClass(2), IsMinor: false);    // D major
        // Open D: low-E + A muted, D 0, G 2, B 3, high-E 2.
        var voicing = new Voicing(
            new[]
            {
                new FretPosition(4, 0), new FretPosition(3, 2),
                new FretPosition(2, 3), new FretPosition(1, 2),
            },
            FirstFret: 0,
            MutedStrings: new[] { 6, 5 });

        FretboardDiagram d = RealizedVoicingDiagram.Build(chord, voicing, key);

        Assert.Equal("D", d.Title);
        Assert.Equal(new[] { 6, 5 }, d.MutedStrings); // low E + A as chrome (low→high walk order)
        Assert.Equal(4, d.Markers.Count);

        Assert.Equal("R", Marker(d, 4).Interval);   // open D → root (not the b3 it would be at a C anchor)
        Assert.Equal("root", Marker(d, 4).Function);
        Assert.Equal("5", Marker(d, 3).Interval);   // G string, 2nd fret → A, the fifth

        FretboardMarker high = Marker(d, 1);         // high E, 2nd fret → F#, the major third
        Assert.Equal("F#", high.Note);
        Assert.Equal("3", high.Interval);
        Assert.Equal("third", high.Function);
    }
}

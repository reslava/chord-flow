using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The CAGED-shape <b>producer</b> of the general <see cref="FretboardDiagram"/> carrier: given a
/// <see cref="CagedShape"/> + a root pitch class, it lights the shape's <b>root anchors</b> (its octave skeleton)
/// and carries the octave <see cref="OctaveShape.Zone">zone</see> as a shaded band — the guitar projection that
/// powers the CAGED Shapes dogfood page for <see cref="OctaveShape"/>. Theory stays upstream: anchors come from
/// <see cref="OctaveShape"/>, the octave-aware label from <see cref="IntervalLattice"/>, the note name from
/// <see cref="NoteSpeller"/>; it adds no geometry of its own. The primary anchor reads <c>1</c>, its octaves
/// <c>8</c> / <c>15</c>, so the page palette can pop the fundamental and dim the octaves.
/// </summary>
public static class CagedShapeDiagram
{
    // Frets of context shown each side of the octave zone, so the zone band reads within the neck rather than
    // filling it edge to edge.
    private const int ZoneMargin = 2;

    /// <summary>
    /// Build the diagram for <paramref name="shape"/> rooted at <paramref name="root"/>, taking the lowest
    /// occurrence within frets <c>0..<paramref name="maxFret"/></c>. Markers are the shape's root anchors (each
    /// labelled <c>1</c>/<c>8</c>/<c>15</c> by octave); the octave zone is carried as the diagram's band and the
    /// fret window is framed to the zone widened by a small margin. Empty diagram if the root never falls on the
    /// shape's primary string in range.
    /// </summary>
    public static FretboardDiagram Build(CagedShape shape, PitchClass root, int maxFret = 15)
    {
        if (maxFret < 0) throw new ArgumentOutOfRangeException(nameof(maxFret));

        var key = new Key(root, IsMinor: false); // spells the note names for this root
        IReadOnlyList<FretPosition> anchors = OctaveShape.AnchorsFor(root, shape, 0, maxFret);
        string rootName = NoteSpeller.Name(root, key);
        string title = $"{rootName} — {shape} shape";

        if (anchors.Count == 0)
        {
            return new FretboardDiagram(
                title, Array.Empty<FretboardMarker>(), Array.Empty<int>(), BarreFret: null, FretMin: null, FretMax: null);
        }

        FretPosition primary = anchors[0]; // RootStrings is primary-first, so anchors[0] is the fundamental
        var markers = new List<FretboardMarker>(anchors.Count);
        int zoneMin = anchors[0].Fret, zoneMax = anchors[0].Fret;
        foreach (FretPosition anchor in anchors)
        {
            PitchClass notePc = Fretboard.PitchClassAt(anchor.String, anchor.Fret);
            LatticeInterval label = IntervalLattice.LabelAt(primary, anchor); // 1 / 8 / 15 by octave
            markers.Add(new FretboardMarker(
                anchor.String, anchor.Fret, NoteSpeller.Name(notePc, key), label.Label, "root", MarkerShape.Circle));

            if (anchor.Fret < zoneMin) zoneMin = anchor.Fret;
            if (anchor.Fret > zoneMax) zoneMax = anchor.Fret;
        }

        return new FretboardDiagram(
            title,
            markers,
            MutedStrings: Array.Empty<int>(),
            BarreFret: null,
            FretMin: Math.Max(0, zoneMin - ZoneMargin), // context window around the zone band
            FretMax: zoneMax + ZoneMargin,
            ZoneFretMin: zoneMin,
            ZoneFretMax: zoneMax);
    }
}

using ChordFlow.Domain;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The five CAGED <b>octave shapes</b> — the root skeleton the caged-system engine hangs chords on. Each shape is
/// the unison/octave special case of the <see cref="IntervalLattice"/>: a stack of the root and its octaves on a
/// fixed set of strings. The <b>only authored data</b> here is the CAGED partition (which strings carry the root,
/// primary first); every fret is <i>derived</i> from <see cref="Fretboard"/>, so there is no second offset table to
/// drift. Pure geometry — no I/O, no UI.
/// </summary>
public static class OctaveShape
{
    // The only authored data: each shape's root strings, ordered primary-first = ascending octave. The k-th string
    // carries the root one octave above the (k-1)-th. alphaTab numbering (1 = high E .. 6 = low E). Every fret is
    // derived from these + Fretboard; the idea's offset numbers (C -2, A +2, ...) are validation examples, not stored.
    private static readonly IReadOnlyDictionary<CagedShape, int[]> RootStringsByShape =
        new Dictionary<CagedShape, int[]>
        {
            [CagedShape.C] = new[] { 5, 2 },
            [CagedShape.A] = new[] { 5, 3 },
            [CagedShape.G] = new[] { 6, 3, 1 },
            [CagedShape.E] = new[] { 6, 4, 1 },
            [CagedShape.D] = new[] { 4, 2 },
        };

    /// <summary>The root strings of <paramref name="shape"/>, ordered primary-first (= ascending octave: the k-th
    /// string carries the root one octave above the primary). The single authored datum of this thread.</summary>
    public static IReadOnlyList<int> RootStrings(CagedShape shape) => RootStringsByShape[shape];

    /// <summary>
    /// The root anchors of <b>one</b> <paramref name="shape"/> instance for <paramref name="root"/>: the primary is
    /// anchored at its lowest occurrence at fret ≥ <paramref name="minFret"/> within
    /// [<paramref name="minFret"/>, <paramref name="maxFret"/>], then each later root string is placed an
    /// <b>ascending octave</b> above it (<c>abs = primaryAbs + k·12</c>). Frets are derived from
    /// <see cref="Fretboard.AbsoluteSemitone"/> — never stored — so a string's in-window unison can never be mistaken
    /// for its octave-up anchor (the D-shape trap). Returns empty if the root does not occur on the primary string
    /// inside the window. Built on <see cref="Fretboard.PositionsFor"/> (no second neck-walk).
    /// </summary>
    public static IReadOnlyList<FretPosition> AnchorsFor(PitchClass root, CagedShape shape, int minFret, int maxFret)
    {
        if (minFret < 0) throw new ArgumentOutOfRangeException(nameof(minFret));
        if (maxFret < minFret) throw new ArgumentOutOfRangeException(nameof(maxFret));

        IReadOnlyList<int> strings = RootStrings(shape);
        int primaryString = strings[0];

        // Lowest occurrence of the root on the primary string at fret ≥ minFret. PositionsFor yields a string's
        // positions in fret-ascending order, so the first match on the primary string is the lowest.
        int? primaryFret = null;
        foreach (FretPosition position in Fretboard.PositionsFor(root, maxFret))
        {
            if (position.String == primaryString && position.Fret >= minFret)
            {
                primaryFret = position.Fret;
                break;
            }
        }

        if (primaryFret is null) return Array.Empty<FretPosition>();

        int primaryAbs = Fretboard.AbsoluteSemitone(primaryString, primaryFret.Value);

        var anchors = new List<FretPosition>(strings.Count);
        for (int k = 0; k < strings.Count; k++)
        {
            int stringNumber = strings[k];
            int desiredAbs = primaryAbs + k * 12;            // the k-th octave of the root
            int fret = desiredAbs - Fretboard.AbsoluteSemitone(stringNumber, 0);
            anchors.Add(new FretPosition(stringNumber, fret));
        }

        return anchors;
    }

    /// <summary>
    /// The <b>octave zone</b> of a <paramref name="shape"/> instance — the [min, max] fret span of its
    /// <see cref="AnchorsFor">anchors</see> for <paramref name="root"/> in the window. Throws if the root does not
    /// occur on the primary string inside the window.
    /// </summary>
    public static OctaveZone Zone(PitchClass root, CagedShape shape, int minFret, int maxFret)
    {
        IReadOnlyList<FretPosition> anchors = AnchorsFor(root, shape, minFret, maxFret);
        if (anchors.Count == 0)
            throw new InvalidOperationException(
                $"{shape} shape has no anchor for the given root within frets [{minFret}, {maxFret}].");

        int min = anchors[0].Fret, max = anchors[0].Fret;
        foreach (FretPosition anchor in anchors)
        {
            if (anchor.Fret < min) min = anchor.Fret;
            if (anchor.Fret > max) max = anchor.Fret;
        }

        return new OctaveZone(min, max);
    }

    /// <summary>
    /// The <b>CAGED boxes</b> of <paramref name="shape"/> — the string-set partition cut by the root strings,
    /// key-independent. A <see cref="CagedBox.IsMain">main</see> box (a complete octave) sits between each
    /// consecutive pair of root strings; <b>partial</b> boxes reach from the outer roots to the neck edges
    /// (strings 6 / 1). Pure function of <see cref="RootStrings"/> — no new data.
    /// </summary>
    public static IReadOnlyList<CagedBox> Boxes(CagedShape shape)
    {
        // Root strings from the bass side (string 6) to the treble side (string 1) = descending string number.
        var roots = RootStrings(shape).OrderByDescending(s => s).ToList();
        int bassRoot = roots[0];
        int trebleRoot = roots[^1];

        var boxes = new List<CagedBox>();

        if (bassRoot < Fretboard.StringCount)                       // headroom below the lowest root
            boxes.Add(new CagedBox(Fretboard.StringCount, bassRoot, IsMain: false));

        for (int i = 0; i < roots.Count - 1; i++)                   // a complete octave between each root pair
            boxes.Add(new CagedBox(roots[i], roots[i + 1], IsMain: true));

        if (trebleRoot > 1)                                         // headroom above the highest root
            boxes.Add(new CagedBox(trebleRoot, 1, IsMain: false));

        return boxes;
    }
}

/// <summary>The [<see cref="MinFret"/>, <see cref="MaxFret"/>] fret span of a shape instance's root anchors.</summary>
public readonly record struct OctaveZone(int MinFret, int MaxFret);

/// <summary>
/// A string-set box inside a CAGED shape: it spans from <see cref="BassString"/> (toward string 6, lower pitch) to
/// <see cref="TrebleString"/> (toward string 1, higher pitch). <see cref="IsMain"/> marks a box bounded by two root
/// strings — a complete octave; a partial box reaches to a neck edge.
/// </summary>
public readonly record struct CagedBox(int BassString, int TrebleString, bool IsMain);

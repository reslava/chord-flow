using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The compact <b>shell-voicing</b> deriver (shell-voicing-derivation, req IN13): the guide-tone shell —
/// root + 3rd + (7th|6th), 5th omitted — derived from the quality formula and the fretboard, with no authored
/// frets. There are exactly two canonical forms (reusing <see cref="CagedShape"/> as the form label):
/// <list type="bullet">
/// <item><b>5th-string root</b> (<see cref="CagedShape.C"/>): root on s5; 3rd on s4 (D), 7th|6th on s3 (G).</item>
/// <item><b>6th-string root</b> (<see cref="CagedShape.E"/>): root on s6; 7th|6th on s4 (D), 3rd on s3 (G); s5 skipped.</item>
/// </list>
/// Each guide tone takes the occurrence on its string <b>nearest the root fret</b> (octave-correct). The root is
/// anchored at the lowest fret in the region whose grip is <b>compact</b> (all three notes within a hand span) —
/// so an open-string root (e.g. A on the open A string) whose guide tones would otherwise land ~12 frets away is
/// pushed up an octave to the playable position (A maj7 → <c>x 12 11 13 x x</c>, not <c>x 0 11 1 x x</c>). This
/// reproduces the maj7-forward / dim7·6·m6-behind placements as a consequence, not a special case. The authored
/// 12-grip table (req IN14) is the golden oracle. Pure — no I/O, no UI.
/// </summary>
public static class ShellDerivation
{
    private const int FifthRootString = 5;   // s5 (A) — the "C" form
    private const int SixthRootString = 6;   // s6 (low E) — the "E" form
    private const int LowGuideString = 4;    // s4 (D)
    private const int HighGuideString = 3;   // s3 (G)

    // A shell is three notes within a hand; a real grip spans a few frets. A far larger span means the root sat
    // on an open string and the guide tones jumped an octave away — reject and try the next (octave-up) anchor.
    private const int MaxShellSpan = 5;

    /// <summary>
    /// Derive the shell for <paramref name="quality"/> in <paramref name="form"/> (<see cref="CagedShape.C"/> or
    /// <see cref="CagedShape.E"/>) at <paramref name="root"/>, anchored at the lowest <b>compact</b> placement in
    /// <c>[<paramref name="minFret"/>, <paramref name="maxFret"/>]</c>. Throws if the quality is not shell-eligible
    /// (no 7th/6th) or the root has no anchor on the form's root string in the region.
    /// </summary>
    public static ChordShape Derive(Quality quality, CagedShape form, PitchClass root, int minFret, int maxFret)
    {
        if (form is not (CagedShape.C or CagedShape.E))
            throw new ArgumentOutOfRangeException(
                nameof(form), form, "A shell has only the C (5th-string root) and E (6th-string root) forms.");

        int third = ToneInterval(quality, ChordToneFunction.Third)
            ?? throw new InvalidOperationException($"{quality} has no third.");
        int guide = SeventhOrSixthInterval(quality)
            ?? throw new InvalidOperationException(
                $"{quality} has no 7th or 6th — shells apply to 7th/6th chords only.");

        int rootString = form == CagedShape.C ? FifthRootString : SixthRootString;
        // The guide tones live on s4 (lower) and s3 (higher). The C form stacks 3rd then 7th/6th; the E form,
        // a string lower in the root, stacks 7th/6th then 3rd.
        (int s4Interval, int s3Interval) = form == CagedShape.C ? (third, guide) : (guide, third);

        IReadOnlyList<int> rootFrets = Fretboard.PositionsFor(root, maxFret)
            .Where(p => p.String == rootString && p.Fret >= minFret)
            .Select(p => p.Fret)
            .OrderBy(f => f)
            .ToList();
        if (rootFrets.Count == 0)
            throw new InvalidOperationException(
                $"No {root.Value} root on string {rootString} within [{minFret}, {maxFret}].");

        // Lowest compact placement; fall back to the lowest if none is compact (e.g. a cramped region).
        ChordShape? fallback = null;
        foreach (int rootFret in rootFrets)
        {
            ChordShape grip = Assemble(quality, form, root, rootString, rootFret, s4Interval, s3Interval, maxFret);
            if (Span(grip) <= MaxShellSpan)
            {
                return grip;
            }

            fallback ??= grip;
        }

        return fallback!;
    }

    private static ChordShape Assemble(
        Quality quality, CagedShape form, PitchClass root, int rootString, int rootFret,
        int s4Interval, int s3Interval, int maxFret)
    {
        int s4Fret = NearestFret(root, s4Interval, LowGuideString, rootFret, maxFret);
        int s3Fret = NearestFret(root, s3Interval, HighGuideString, rootFret, maxFret);

        var strings = new List<ChordShapeString>(Fretboard.StringCount);
        for (int s = Fretboard.StringCount; s >= 1; s--)
        {
            strings.Add(s switch
            {
                _ when s == rootString => new ChordShapeString(s, rootFret, 0),
                LowGuideString => new ChordShapeString(s, s4Fret, s4Interval),
                HighGuideString => new ChordShapeString(s, s3Fret, s3Interval),
                _ => ChordShapeString.Muted(s),
            });
        }

        var sounded = strings.Where(s => !s.IsMuted).Select(s => s.Fret!.Value).ToList();
        int boxMin = sounded.Min();
        int boxMax = sounded.Max();
        Finger anchorFinger = rootFret >= boxMin && rootFret <= boxMax
            ? AnchorFinger.Derive(rootFret, boxMin, boxMax)
            : Finger.Index;

        return new ChordShape(quality, form, strings, anchorFinger, new OctaveZone(boxMin, boxMax));
    }

    private static int Span(ChordShape grip)
    {
        var frets = grip.Strings.Where(s => !s.IsMuted).Select(s => s.Fret!.Value).ToList();
        return frets.Max() - frets.Min();
    }

    // The fret on stringNumber sounding (root + interval), whose occurrence is closest to referenceFret — picking
    // the right octave; ties prefer the lower fret. Bounded by 0..maxFret (the behind/forward reach falls out).
    private static int NearestFret(PitchClass root, int interval, int stringNumber, int referenceFret, int maxFret)
    {
        int targetPc = Mod12(root.Value + interval);
        var frets = Fretboard.PositionsFor(new PitchClass(targetPc), maxFret)
            .Where(p => p.String == stringNumber)
            .Select(p => p.Fret)
            .ToList();
        if (frets.Count == 0)
            throw new InvalidOperationException(
                $"No fret for interval {interval} on string {stringNumber} within 0..{maxFret}.");

        return frets.OrderBy(f => Math.Abs(f - referenceFret)).ThenBy(f => f).First();
    }

    private static int? ToneInterval(Quality quality, ChordToneFunction function) =>
        ChordTones.Of(new Chord(new PitchClass(0), quality))
            .Where(t => t.Function == function)
            .Select(t => (int?)t.Interval)
            .FirstOrDefault();

    private static int? SeventhOrSixthInterval(Quality quality) =>
        ChordTones.Of(new Chord(new PitchClass(0), quality))
            .Where(t => t.Function is ChordToneFunction.Seventh or ChordToneFunction.Sixth)
            .Select(t => (int?)t.Interval)
            .FirstOrDefault();

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}

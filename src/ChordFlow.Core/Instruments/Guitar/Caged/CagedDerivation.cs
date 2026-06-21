using ChordFlow.Domain;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The CAGED <b>derivation engine</b> (req <c>IN1</c>): <c>derive(quality, shape, root, neckRegion) → ChordShape</c>,
/// computed from the locked substrates with no authored fret tables. The pipeline:
/// <list type="number">
/// <item>place the root anchors + octave zone of the shape (<see cref="OctaveShape"/>);</item>
/// <item>mute the strings below the bass root; the rest are played;</item>
/// <item>enumerate each played string's chord-tone candidates within the reach window
///   (<see cref="IntervalLattice"/> × <see cref="HandReach.CandidateWindow"/>);</item>
/// <item>pick one tone per string with the whole-box <see cref="CandidateSelector"/> (B-string tax, zone
///   containment, full chord spelling, tightest grip);</item>
/// <item>derive the <see cref="Finger">anchor finger</see> from the root's rank in the realized box.</item>
/// </list>
/// Pure — no I/O, no UI. The 34 authored voicings are the golden oracle for <see cref="Derive"/>.
/// </summary>
public static class CagedDerivation
{
    /// <summary>Max fret span (high − low) of a derived <b>chord</b> grip — width 4 = the 4-finger hand (chat-002).
    /// Chords only; scales/arpeggios use the unclamped reach table.</summary>
    public const int MaxChordWidth = 4;
    private const int MaxChordSpan = MaxChordWidth - 1;

    /// <summary>
    /// Derive the <see cref="ChordShape"/> for <paramref name="quality"/> in the CAGED <paramref name="shape"/> at
    /// <paramref name="root"/>, placed in the neck region <c>[<paramref name="minFret"/>, <paramref name="maxFret"/>]</c>
    /// (the region whose lowest occurrence of the root on the shape's primary string anchors the grip). Throws if
    /// the shape has no anchor in the region or no valid grip can be spelled.
    /// </summary>
    public static ChordShape Derive(Quality quality, CagedShape shape, PitchClass root, int minFret, int maxFret)
    {
        IReadOnlyList<FretPosition> anchors = OctaveShape.AnchorsFor(root, shape, minFret, maxFret);
        if (anchors.Count == 0)
            throw new InvalidOperationException($"{shape} shape has no anchor for the root within [{minFret}, {maxFret}].");

        OctaveZone zone = OctaveShape.Zone(root, shape, minFret, maxFret);
        FretPosition rootOrigin = anchors[0];                       // any root position carries the root pitch class

        int bassRoot = OctaveShape.RootStrings(shape).Max();        // lowest-pitch root string (toward string 6)
        int bassFret = anchors.First(a => a.String == bassRoot).Fret;

        // Anchor direction: is the bass root the LOWEST octave anchor (index-anchored — box stacks UP from it) or
        // the HIGHEST (pinky-anchored — box stacks DOWN)? Derived from the anchors, not authored. Every authored
        // grip is root-in-the-bass and reaches only to the anchor finger's side, so the box stays on the shape.
        bool stacksUp = bassFret == anchors.Min(a => a.Fret);

        // Reach window from the bass root: the box may extend in the anchor finger's reach direction only, as far
        // as the hand stretches — index reaches up (+ahead), pinky reaches down (−behind). This is where the reach
        // table (HandReach) finally bounds placement; within it, candidate selection maximizes tone weight.
        // Chord cap (chat-002): a chord grip spans at most MaxChordWidth frets (the 4-finger hand). The width cap is
        // enforced in CandidateSelector (SpanWith) on the realized grip; the window only bounds candidate enumeration.
        // The bass root is the grip extreme, so the box reaches only in the anchor finger's stacking direction.
        // behind-1 (T4, chat-002): the fully-symmetric dim7 is the one quality whose nearest 7th lands a fret *below*
        // the bass root, so up-stacked dim7 grips get the index's behind-1 reach (one "stretch-back" fret). The
        // selector lets that fret voice only an uncovered tone, never a doubling — so it grabs the low bb7 without
        // dragging colour tones below the bass on every other quality. Other qualities reach forward only.
        int reachAhead = Math.Min(HandReach.Of(Finger.Index).Ahead, MaxChordSpan);
        int reachBehind = Math.Min(HandReach.Of(Finger.Pinky).Behind, MaxChordSpan);
        bool allowStretchBack = stacksUp && quality == Quality.Diminished7;
        int stretchBack = allowStretchBack ? HandReach.Of(Finger.Index).Behind : 0;
        FretWindow window = stacksUp
            ? new FretWindow(Math.Max(0, bassFret - stretchBack), bassFret + reachAhead)
            : new FretWindow(Math.Max(0, bassFret - reachBehind), bassFret);

        IReadOnlyList<int> distinctTones = QualityIntervals.Intervals(quality).Distinct().ToList();

        // Candidates per played string (bassRoot .. 1): every chord tone that lands on that string in the window.
        var candidatesByString = new Dictionary<int, IReadOnlyList<ToneCandidate>>();
        for (int s = bassRoot; s >= 1; s--)
        {
            var candidates = new List<ToneCandidate>();
            foreach (int tone in distinctTones)
            {
                foreach (FretPosition position in
                         IntervalLattice.PositionsOfInterval(rootOrigin, tone, window.MinFret, window.MaxFret))
                {
                    if (position.String != s) continue;
                    candidates.Add(new ToneCandidate(position.Fret, tone));
                }
            }

            // The bass-most played string carries the root at its anchor — every authored grip is root-position.
            if (s == bassRoot)
            {
                candidates = candidates.Where(c => c.Semitones == 0 && c.Fret == bassFret).ToList();
            }

            if (candidates.Count > 0)
            {
                candidatesByString[s] = candidates;
            }
        }

        IReadOnlySet<int> rootStrings = OctaveShape.RootStrings(shape).ToHashSet();
        int? stretchBackFret = allowStretchBack ? bassFret - stretchBack : null;
        IReadOnlyDictionary<int, ToneCandidate> chosen =
            CandidateSelector.Select(candidatesByString, rootStrings, stretchBackFret, MaxChordSpan);
        if (chosen.Count == 0)
            throw new InvalidOperationException($"No {quality} grip could be stacked in the {shape} shape.");

        // Anchor box = the fretted notes only (open strings take no finger, so they don't shape the hand span).
        IReadOnlyList<int> frettedFrets = chosen.Values.Where(c => c.Fret > 0).Select(c => c.Fret).ToList();
        if (frettedFrets.Count == 0) frettedFrets = chosen.Values.Select(c => c.Fret).ToList();
        int boxMin = frettedFrets.Min();
        int boxMax = frettedFrets.Max();

        // Anchor finger: rank of the root within the realized box. Prefer the bass-most string that sounds the root.
        int anchorFret = chosen
            .Where(kv => kv.Value.Semitones == 0)
            .OrderByDescending(kv => kv.Key)
            .Select(kv => kv.Value.Fret)
            .DefaultIfEmpty(anchors[0].Fret)
            .First();
        Finger anchorFinger = AnchorFinger.Derive(anchorFret, boxMin, boxMax);

        // Assemble one entry per string, low-E→high-E (6 → 1): muted below the bass root, played at/above it.
        var strings = new List<ChordShapeString>(Fretboard.StringCount);
        for (int s = Fretboard.StringCount; s >= 1; s--)
        {
            if (s > bassRoot || !chosen.TryGetValue(s, out ToneCandidate tone))
            {
                strings.Add(ChordShapeString.Muted(s));
            }
            else
            {
                strings.Add(new ChordShapeString(s, tone.Fret, tone.Semitones));
            }
        }

        return new ChordShape(quality, shape, strings, anchorFinger, zone);
    }
}

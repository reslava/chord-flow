namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// One candidate note for a played string: a <see cref="Fret"/> sounding the chord tone
/// <see cref="Semitones"/> (interval from the root).
/// </summary>
public readonly record struct ToneCandidate(int Fret, int Semitones);

/// <summary>
/// How much a chord tone is "worth" when a string has a free choice (req <c>IN5</c> / Rule 1): a CAGED grip should
/// keep roots on its root strings, then favour the colour tones. Root 100 &gt; 3rd 70 &gt; 7th 50 &gt; 5th 30. Other
/// tensions (9/11/13…) score 0 for <i>duplication</i> — they are kept by the "voice every distinct tone" pass (each
/// appears at least once), not by being doubled. Interval roles are read by semitone class.
/// </summary>
public static class ChordToneWeight
{
    /// <summary>The duplication weight of the chord tone at <paramref name="semitones"/> from the root.</summary>
    public static int Of(int semitones)
    {
        int pitchClass = semitones % 12;
        if (pitchClass < 0) pitchClass += 12;

        return pitchClass switch
        {
            0 => 100,               // root
            3 or 4 => 70,           // 3rd (b3 / 3)
            9 or 10 or 11 => 50,    // 7th (bb7 / b7 / 7)
            6 or 7 or 8 => 30,      // 5th (b5 / 5 / #5)
            _ => 0,                 // tensions — presence guaranteed by the all-tones pass, no doubling bonus
        };
    }
}

/// <summary>
/// The <b>bass-up tone stacker</b> (req <c>IN4</c> / Rafa's Rule, chat-001 2026-06-21): build a CAGED grip by
/// stacking tones from the bass root upward, one string at a time. The bass string sounds the root; then, for each
/// higher string, pick the highest-<see cref="ChordToneWeight">weight</see> chord tone that fits the reach window
/// and is <b>not yet voiced</b>. Once every distinct tone has been placed, the remaining strings are filled with the
/// highest-weight tone that fits (roots land where they reach, the next octave; otherwise a colour tone repeats).
/// <para>
/// This "prefer an uncovered tone, repeat only when no new tone fits" rule is what reconciles the two hard cases:
/// in m7·G the root keeps string 3 (the 5th can't reach that string in the window, so the octave root falls there),
/// while in dom7·E the b7 takes the octave-root string 4 (it fits, and the root is already framed below). The
/// caller offers only candidates inside the anchor finger's reach window from the bass root, so the box never
/// drifts off the shape. A string with no candidate in the window is left out (muted).
/// </para>
/// Pure — no I/O, no UI.
/// </summary>
public static class CandidateSelector
{
    /// <summary>
    /// Stack one tone per played string, bass → treble. <paramref name="candidatesByString"/> maps each played
    /// string to its window-eligible <see cref="ToneCandidate"/>s (the bass string already filtered to the root).
    /// <paramref name="rootStrings"/> are the shape's root strings (T4: a root prefers a root string). The grip is
    /// kept within <paramref name="maxSpan"/> frets of what's placed (the width-4 cap). <paramref name="stretchBackFret"/>,
    /// when set, is the single fret reached only by the index's <i>behind-1</i> stretch (up-stackers): a tone there may
    /// only voice an <b>uncovered</b> tone — you stretch back to grab a missing tone, never to double one (T4).
    /// Returns the chosen tone per string; a string with no candidate is omitted (muted).
    /// </summary>
    public static IReadOnlyDictionary<int, ToneCandidate> Select(
        IReadOnlyDictionary<int, IReadOnlyList<ToneCandidate>> candidatesByString,
        IReadOnlySet<int> rootStrings,
        int? stretchBackFret,
        int maxSpan)
    {
        var placed = new HashSet<int>();
        var result = new Dictionary<int, ToneCandidate>();

        // Bass → treble = descending string number (string 6 is the bass, string 1 the treble).
        foreach (int stringNumber in candidatesByString.Keys.OrderByDescending(s => s))
        {
            IReadOnlyList<ToneCandidate> candidates = candidatesByString[stringNumber];
            if (candidates.Count == 0)
            {
                continue; // no chord tone reaches this string in the window — leave it muted
            }

            // Width-4 cap: keep the realized grip within maxSpan frets of what's placed so far. Never let this empty a
            // placeable string (completeness wins over the cap, which the reach window already mostly enforces).
            List<ToneCandidate> pool = result.Count == 0
                ? candidates.ToList()
                : candidates.Where(c => SpanWith(result.Values, c.Fret) <= maxSpan).ToList();
            if (pool.Count == 0) pool = candidates.ToList();

            List<ToneCandidate> uncovered = pool.Where(c => !placed.Contains(PitchClass(c.Semitones))).ToList();

            ToneCandidate pick;
            if (uncovered.Count > 0)
            {
                // Still tones to voice: take the highest-weight uncovered tone that fits — the behind-1 stretch-back
                // fret is allowed here (this is exactly the "reach back for a missing tone" case).
                pick = uncovered
                    .OrderByDescending(c => ChordToneWeight.Of(c.Semitones))
                    .ThenBy(c => c.Fret)
                    .First();
            }
            else
            {
                // Fill step (every distinct tone already voiced): this string only doubles a tone. The behind-1
                // stretch-back fret may not double (only reach a missing tone), so drop it from the fill pool.
                List<ToneCandidate> fill = stretchBackFret is int sb
                    ? pool.Where(c => c.Fret != sb).ToList()
                    : pool;
                if (fill.Count == 0) fill = pool;

                int boxMin = result.Values.Min(c => c.Fret);
                int boxMax = result.Values.Max(c => c.Fret);

                // T4 — a root belongs on a root string: if this is a root string and a root double is reachable, keep
                // it (preserves the CAGED skeleton) even over a tighter non-root double. Otherwise the most compact
                // double, then weight, then lowest fret. (chat-002, line 491 + T4.)
                pick = rootStrings.Contains(stringNumber) && fill.Any(c => PitchClass(c.Semitones) == 0)
                    ? fill.Where(c => PitchClass(c.Semitones) == 0)
                        .OrderBy(c => StretchBeyond(c.Fret, boxMin, boxMax)).ThenBy(c => c.Fret).First()
                    : fill
                        .OrderBy(c => StretchBeyond(c.Fret, boxMin, boxMax))
                        .ThenByDescending(c => ChordToneWeight.Of(c.Semitones))
                        .ThenBy(c => c.Fret)
                        .First();
            }

            result[stringNumber] = pick;
            placed.Add(PitchClass(pick.Semitones));
        }

        return result;
    }

    private static int PitchClass(int semitones) => ((semitones % 12) + 12) % 12;

    /// <summary>How far <paramref name="fret"/> stretches past the box <c>[<paramref name="boxMin"/>, <paramref name="boxMax"/>]</c> placed so far (0 if it lands inside).</summary>
    private static int StretchBeyond(int fret, int boxMin, int boxMax) =>
        fret < boxMin ? boxMin - fret : fret > boxMax ? fret - boxMax : 0;

    /// <summary>The fret span the grip would have if <paramref name="fret"/> joined the already-<paramref name="placed"/> notes.</summary>
    private static int SpanWith(IEnumerable<ToneCandidate> placed, int fret)
    {
        int min = fret, max = fret;
        foreach (ToneCandidate c in placed)
        {
            if (c.Fret < min) min = c.Fret;
            if (c.Fret > max) max = c.Fret;
        }
        return max - min;
    }
}

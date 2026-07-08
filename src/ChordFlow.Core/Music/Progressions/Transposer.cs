using ChordFlow.Music.Harmony;
namespace ChordFlow.Music.Progressions;

/// <summary>
/// One realized chord span: a concrete <see cref="Chord"/> placed at <see cref="StartTick"/> for
/// <see cref="DurationTicks"/> on the 48-PPQ grid. The key-resolved counterpart of <see cref="ChordSpan"/>.
/// </summary>
/// <param name="Chord">The concrete, key-resolved chord.</param>
/// <param name="StartTick">Bar-relative start on the 48-PPQ grid.</param>
/// <param name="DurationTicks">Span length in ticks.</param>
/// <param name="Degree">The originating key-independent <see cref="RomanDegree"/>, preserved so a Song's
/// degree-scoped <c>voice</c> default can match after transposition (the concrete chord alone has lost the
/// degree). Defaults to <c>default</c> for the legacy render paths that don't need it.</param>
/// <param name="VoicingAnnotation">The per-chord <c>{…}</c> annotation carried verbatim from
/// <see cref="ChordSpan.VoicingAnnotation"/> — opaque raw spec text (design D9), consumed by the Features layer.</param>
public readonly record struct RealizedSpan(
    Chord Chord,
    int StartTick,
    int DurationTicks,
    RomanDegree Degree = default,
    string? VoicingAnnotation = null);

/// <summary>
/// One realized bar: the ordered <see cref="RealizedSpan"/>s for a <see cref="HarmonicBar"/> after
/// transposition into a key. <see cref="ChordCovering"/> is the renderer's slot → chord primitive.
/// </summary>
public sealed record RealizedBar(IReadOnlyList<RealizedSpan> Spans)
{
    /// <summary>The chord whose span's <c>[StartTick, StartTick + DurationTicks)</c> contains <paramref name="tick"/>.</summary>
    public Chord ChordCovering(int tick) => SpanCovering(tick).Chord;

    /// <summary>
    /// The <see cref="RealizedSpan"/> whose <c>[StartTick, StartTick + DurationTicks)</c> contains
    /// <paramref name="tick"/> — the per-occurrence peer of <see cref="ChordCovering"/> that carries the span's
    /// voicing annotation, so the renderer can resolve a per-chord <c>{…}</c> override (not just the chord value).
    /// </summary>
    public RealizedSpan SpanCovering(int tick)
    {
        foreach (RealizedSpan span in Spans)
        {
            if (tick >= span.StartTick && tick < span.StartTick + span.DurationTicks)
            {
                return span;
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(tick), tick, "No realized chord span covers the tick.");
    }
}

/// <summary>
/// Pure transposition: realizes a key-independent <see cref="Progression"/> into concrete
/// <see cref="Chord"/>s for a given <see cref="Key"/> (or <see cref="Scale"/>). No I/O, no state.
/// The scale-degree offsets live in <see cref="Scale"/>; this type just maps degrees through it.
/// </summary>
public static class Transposer
{
    /// <summary>
    /// Legacy one-chord-per-bar view: maps each bar's first <see cref="ChordSpan"/> to a concrete chord
    /// in <paramref name="key"/>. Exact for single-span bars (today's progressions); multi-chord bars are
    /// realized span-by-span via <see cref="RealizeBars(Progression, Key)"/>, which the renderer uses.
    /// </summary>
    public static Chord[] Realize(Progression progression, Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Realize(progression, Scale.ForKey(key), key);
    }

    /// <summary>
    /// Legacy one-chord-per-bar view in <paramref name="scale"/> (see <see cref="Realize(Progression, Key)"/>).
    /// </summary>
    public static Chord[] Realize(Progression progression, Scale scale) => Realize(progression, scale, key: null);

    private static Chord[] Realize(Progression progression, Scale scale, Key? key)
    {
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(scale);

        var chords = new Chord[progression.Bars.Count];
        for (int i = 0; i < progression.Bars.Count; i++)
        {
            RomanDegree degree = progression.Bars[i].Spans[0].Degree;
            chords[i] = ChordFor(degree, scale, key);
        }

        return chords;
    }

    /// <summary>
    /// Realizes the full harmonic-rhythm layer: every <see cref="ChordSpan"/> of every bar to a concrete
    /// chord with its bar-relative <see cref="RealizedSpan.StartTick"/> and duration, in <paramref name="key"/>.
    /// </summary>
    public static IReadOnlyList<RealizedBar> RealizeBars(Progression progression, Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return RealizeBars(progression, Scale.ForKey(key), key);
    }

    /// <summary>Realizes the full harmonic-rhythm layer in <paramref name="scale"/>.</summary>
    public static IReadOnlyList<RealizedBar> RealizeBars(Progression progression, Scale scale) =>
        RealizeBars(progression, scale, key: null);

    private static IReadOnlyList<RealizedBar> RealizeBars(Progression progression, Scale scale, Key? key)
    {
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(scale);

        var bars = new RealizedBar[progression.Bars.Count];
        for (int i = 0; i < progression.Bars.Count; i++)
        {
            HarmonicBar bar = progression.Bars[i];
            var spans = new RealizedSpan[bar.Spans.Count];
            int start = 0;
            for (int j = 0; j < bar.Spans.Count; j++)
            {
                ChordSpan span = bar.Spans[j];
                spans[j] = new RealizedSpan(
                    ChordFor(span.Degree, scale, key), start, span.DurationTicks, span.Degree, span.VoicingAnnotation);
                start += span.DurationTicks;
            }

            bars[i] = new RealizedBar(spans);
        }

        return bars;
    }

    // The musical alphabet, used to advance the tonic's letter by (degree - 1) steps. The major and
    // natural-minor scales both walk these seven letters in order, so the degree number alone fixes the
    // root's letter — independent of which accidental the key or the chromatic alteration adds.
    private const string Alphabet = "CDEFGAB";

    // Pitch class of each bare letter, in Alphabet order (C=0, D=2, E=4, F=5, G=7, A=9, B=11).
    private static readonly int[] LetterPitchClasses = { 0, 2, 4, 5, 7, 9, 11 };

    // The root pitch class is the scale degree's pitch class shifted by any chromatic accidental; the
    // quality carries straight through from the degree (e.g. Dominant7 for blues). When a key is given,
    // the root is spelled letter-pure from the written degree (design §5) and carried on the chord;
    // the scale-only overloads leave RootSpelling null and let the key-table spell it at display time.
    private static Chord ChordFor(RomanDegree degree, Scale scale, Key? key)
    {
        int diatonicPc = scale.DegreePitchClass(degree.Degree).Value;
        int rootPc = Mod12(diatonicPc + AccidentalOffset(degree.Accidental));

        // Only an accidental'd degree carries a letter-pure RootSpelling (IN5); a diatonic degree leaves it
        // null so ChordSymbol falls back to the key-table and existing output stays byte-identical (C2).
        NoteName? spelling = key is not null && degree.Accidental != Accidental.Natural
            ? SpellRoot(degree, key, rootPc)
            : null;
        return new Chord(new PitchClass(rootPc), degree.Quality, spelling);
    }

    // Letter-pure spelling: the letter is the tonic's letter advanced (degree - 1) places through the
    // musical alphabet, and the accidental is whatever turns that letter into the sounding pitch — even
    // when that yields a rare F♭/B♯/double accidental. The written degree, not the key, names the root.
    private static NoteName SpellRoot(RomanDegree degree, Key key, int rootPc)
    {
        char tonicLetter = NoteSpeller.Name(key.Tonic, key)[0];
        int letterIndex = (Alphabet.IndexOf(tonicLetter) + degree.Degree - 1) % Alphabet.Length;
        int naturalPc = LetterPitchClasses[letterIndex];

        // Signed semitone distance letter → pitch, normalized to the nearest spelling (e.g. -1 = 'b',
        // +1 = '#'); a value > 6 wraps to a small negative so we never spell, say, B as "B#######".
        int accidental = Mod12(rootPc - naturalPc);
        if (accidental > 6)
        {
            accidental -= 12;
        }

        return new NoteName(Alphabet[letterIndex], accidental);
    }

    private static int AccidentalOffset(Accidental accidental) => accidental switch
    {
        Accidental.Sharp => 1,
        Accidental.Flat => -1,
        _ => 0,
    };

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}

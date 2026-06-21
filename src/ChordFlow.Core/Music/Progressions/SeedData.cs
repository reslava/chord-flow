using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
namespace ChordFlow.Music.Progressions;

/// <summary>
/// Hand-authored MVP domain constants: the 12-bar blues progression, the three rhythm patterns (tick-grid
/// model), and the 12 major keys. Pure constants — no I/O. These are the live <see cref="Progression"/> /
/// <see cref="RhythmPattern"/> values used by rendering and tests; the <b>persisted</b> built-in content
/// (the rows seeded on first run) is no longer authored here — it ships as the on-disk default content pack
/// (<c>Content/default-pack/</c>) imported via <c>Features/Packs/DefaultPack</c> (IN6: content is data, not
/// code). The DSL strings below are the same ones the default pack's <c>.dsl</c> files carry.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// 12-bar blues: <c>I I I I  IV IV  I I  V IV I V</c>, all Dominant7 (blues convention). Each degree
    /// becomes one single-span <see cref="HarmonicBar"/> (a full-bar <see cref="ChordSpan"/> of 192 ticks)
    /// via the degree constructor — a single-chord bar = one full-bar span (C4), so the rendered output is
    /// byte-identical to the pre-harmonic-rhythm model.
    /// </summary>
    public static readonly Progression TwelveBarBlues = new(
        "12bar_blues",
        "12-Bar Blues",
        new RomanDegree[]
        {
            new(1, Quality.Dominant7), new(1, Quality.Dominant7), new(1, Quality.Dominant7), new(1, Quality.Dominant7),
            new(4, Quality.Dominant7), new(4, Quality.Dominant7),
            new(1, Quality.Dominant7), new(1, Quality.Dominant7),
            new(5, Quality.Dominant7), new(4, Quality.Dominant7), new(1, Quality.Dominant7), new(5, Quality.Dominant7),
        });

    // Rhythm patterns on the 48-PPQ tick grid. The DSL string is the single source of truth: each pattern is
    // parsed from the same sustain-literal DSL the default pack's rhythm file carries, so the in-memory
    // constant and the persisted definition can never drift. A struck note rings to the next attack/rest or
    // the bar end (the sustain rule), and the quantizer coalesces those beat-aligned rings into whole/half
    // notes (Beat 1 → a whole note, Beats 1 & 3 → two halves).
    private const string Beat1Dsl = "X...............";    // one whole-bar ring
    private const string Beat1And3Dsl = "X.......X......."; // two half notes
    private const string QuartersDsl = "X...X...X...X...";  // four quarters

    /// <summary>Strike on beat 1, ringing the whole bar.</summary>
    public static readonly RhythmPattern Beat1 =
        RhythmPatternParser.Parse("beat_1", "Beat 1", Beat1Dsl, TimeSignature.FourFour);

    /// <summary>Strike on beats 1 and 3 — two half notes.</summary>
    public static readonly RhythmPattern Beat1And3 =
        RhythmPatternParser.Parse("beat_1_3", "Beats 1 & 3", Beat1And3Dsl, TimeSignature.FourFour);

    /// <summary>Strike on every quarter beat.</summary>
    public static readonly RhythmPattern Quarters =
        RhythmPatternParser.Parse("quarters", "Quarters", QuartersDsl, TimeSignature.FourFour);

    /// <summary>The three MVP rhythm patterns, in UI order.</summary>
    public static readonly IReadOnlyList<RhythmPattern> RhythmPatterns = new[] { Beat1, Beat1And3, Quarters };

    /// <summary>All 12 keys as major, ordered by tonic pitch class 0 (C) .. 11 (B).</summary>
    public static readonly IReadOnlyList<Key> AllMajorKeys =
        Enumerable.Range(0, 12).Select(v => new Key(new PitchClass(v), false)).ToArray();
}

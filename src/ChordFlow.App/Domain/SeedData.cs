namespace ChordFlow.Domain;

/// <summary>
/// A code-authored built-in progression: its stable id, display name and canonical Nashville DSL.
/// The persistence layer seeds these as <c>ProgressionEntity</c> rows with <c>Origin = BuiltIn</c>;
/// keeping them here (pure Domain, no I/O) lets the defaults be tested without a database.
/// </summary>
public sealed record ProgressionDefinition(string Id, string Name, string Dsl);

/// <summary>
/// Hand-authored MVP seed data: the single supported progression (12-bar blues), the three rhythm
/// patterns (tick-grid model), and the 12 major keys. Pure constants — no I/O.
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

    // Rhythm patterns on the tick grid (48 PPQ): a quarter = 48, the 4/4 bar = 192. Each hit is one
    // quarter; the quantizer fills the gaps with quarter rests.

    /// <summary>Hit on beat 1 only.</summary>
    public static readonly RhythmPattern Beat1 = new(
        "beat_1",
        "Beat 1",
        new[] { RhythmEvent.Hit(0, TickGrid.Ppq) },
        TimeSignature.FourFour);

    /// <summary>Hits on beats 1 and 3.</summary>
    public static readonly RhythmPattern Beat1And3 = new(
        "beat_1_3",
        "Beats 1 & 3",
        new[]
        {
            RhythmEvent.Hit(0, TickGrid.Ppq),
            RhythmEvent.Hit(2 * TickGrid.Ppq, TickGrid.Ppq),
        },
        TimeSignature.FourFour);

    /// <summary>Hits on every quarter beat.</summary>
    public static readonly RhythmPattern Quarters = new(
        "quarters",
        "Quarters",
        new[]
        {
            RhythmEvent.Hit(0, TickGrid.Ppq),
            RhythmEvent.Hit(TickGrid.Ppq, TickGrid.Ppq),
            RhythmEvent.Hit(2 * TickGrid.Ppq, TickGrid.Ppq),
            RhythmEvent.Hit(3 * TickGrid.Ppq, TickGrid.Ppq),
        },
        TimeSignature.FourFour);

    /// <summary>The three MVP rhythm patterns, in UI order.</summary>
    public static readonly IReadOnlyList<RhythmPattern> RhythmPatterns = new[] { Beat1, Beat1And3, Quarters };

    /// <summary>
    /// The built-in progression default set, seeded on first run with <c>Origin = BuiltIn</c> (IN11). The
    /// <see cref="ProgressionDefinition.Dsl"/> for <c>12bar_blues</c> matches <see cref="TwelveBarBlues"/>;
    /// the jazz-blues turnaround exercises multi-chord bars (Half/Half and the 2-chord turnaround bars).
    /// </summary>
    public static readonly IReadOnlyList<ProgressionDefinition> BuiltInProgressions = new[]
    {
        new ProgressionDefinition("12bar_blues", "12-Bar Blues", "17 17 17 17 47 47 17 17 57 47 17 57"),
        new ProgressionDefinition("jazz_blues_turnaround", "Jazz Blues Turnaround", "2-7 57 17_67 2-7_57"),
    };

    /// <summary>All 12 keys as major, ordered by tonic pitch class 0 (C) .. 11 (B).</summary>
    public static readonly IReadOnlyList<Key> AllMajorKeys =
        Enumerable.Range(0, 12).Select(v => new Key(new PitchClass(v), false)).ToArray();
}

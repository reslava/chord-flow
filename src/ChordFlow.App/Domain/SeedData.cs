namespace ChordFlow.Domain;

/// <summary>
/// Hand-authored MVP seed data: the single supported progression (12-bar blues),
/// the three rhythm patterns, and the 12 major keys. Pure constants — no I/O.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// 12-bar blues: <c>I I I I  IV IV  I I  V IV I V</c>, all Dominant7 (blues convention).
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

    /// <summary>Hit on beat 1 only, rests on 2/3/4 (all quarters).</summary>
    public static readonly RhythmPattern Beat1 = new(
        "beat_1",
        "Beat 1",
        new Beat[]
        {
            new(Duration.Quarter, true),
            new(Duration.Quarter, false),
            new(Duration.Quarter, false),
            new(Duration.Quarter, false),
        });

    /// <summary>Hits on beats 1 and 3, rests on 2/4 (all quarters).</summary>
    public static readonly RhythmPattern Beat1And3 = new(
        "beat_1_3",
        "Beats 1 & 3",
        new Beat[]
        {
            new(Duration.Quarter, true),
            new(Duration.Quarter, false),
            new(Duration.Quarter, true),
            new(Duration.Quarter, false),
        });

    /// <summary>Hits on every quarter beat.</summary>
    public static readonly RhythmPattern Quarters = new(
        "quarters",
        "Quarters",
        new Beat[]
        {
            new(Duration.Quarter, true),
            new(Duration.Quarter, true),
            new(Duration.Quarter, true),
            new(Duration.Quarter, true),
        });

    /// <summary>The three MVP rhythm patterns, in UI order.</summary>
    public static readonly IReadOnlyList<RhythmPattern> RhythmPatterns = new[] { Beat1, Beat1And3, Quarters };

    /// <summary>All 12 keys as major, ordered by tonic pitch class 0 (C) .. 11 (B).</summary>
    public static readonly IReadOnlyList<Key> AllMajorKeys =
        Enumerable.Range(0, 12).Select(v => new Key(new PitchClass(v), false)).ToArray();
}

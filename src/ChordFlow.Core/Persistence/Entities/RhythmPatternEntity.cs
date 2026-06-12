namespace ChordFlow.Persistence.Entities;

/// <summary>
/// Persisted rhythm-pattern <b>definition</b> — the canonical Rhythm-DSL <see cref="Dsl"/> string is the
/// single serialization (constraint C1: alphaTex and the parsed tick grid are never stored, they are
/// regenerated on load). Mirrors <see cref="ProgressionEntity"/>'s "store the definition, regenerate on
/// load" pattern, but rhythm patterns carry <b>no</b> catalog metadata (genre/subgenre/tags — EX3): they
/// aren't genre-filtered. The <see cref="TsNumerator"/>/<see cref="TsDenominator"/> pair records the
/// pattern's time signature (4/4 only today, stored so non-4/4 is an additive change). Load =
/// <c>RhythmPatternParser.Parse(Dsl, ts)</c> → <c>RhythmPattern</c>.
/// </summary>
public sealed class RhythmPatternEntity : IOriginated
{
    /// <summary>Stable id and primary key. Human slug for built-ins (e.g. <c>beat_1</c>), GUID for user patterns.</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name (e.g. <c>Beat 1</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Canonical Rhythm-DSL string — the single serialization (e.g. <c>X...X...X...X...</c>).</summary>
    public string Dsl { get; set; } = "";

    /// <summary>Time-signature numerator (4 for 4/4). Stored so a future non-4/4 pattern is additive.</summary>
    public int TsNumerator { get; set; } = 4;

    /// <summary>Time-signature denominator (4 for 4/4).</summary>
    public int TsDenominator { get; set; } = 4;

    /// <summary>Provenance: built-in / user-defined / pack (stored as its name).</summary>
    public Origin Origin { get; set; }

    /// <summary>For <see cref="Origin.Pack"/>, the source pack's id; null for built-in/user-defined.</summary>
    public string? PackId { get; set; }

    /// <summary>When this definition was first saved (UTC).</summary>
    public DateTime CreatedUtc { get; set; }
}

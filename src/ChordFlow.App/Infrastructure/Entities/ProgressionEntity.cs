using ChordFlow.Domain;

namespace ChordFlow.Infrastructure.Entities;

/// <summary>
/// Persisted progression <b>definition</b> — the canonical Nashville <see cref="Dsl"/> string is the v1
/// serialization (constraint C5: a future richer form can add a <c>spans_json</c> column or normalized
/// tables without losing this string). Load = <c>ProgressionParser.Parse(Dsl)</c> → realize → render;
/// alphaTex is never stored. Mirrors the <see cref="ExerciseEntity"/> "store the definition, regenerate
/// on load" pattern.
/// </summary>
public sealed class ProgressionEntity
{
    /// <summary>Stable id and primary key. Human slug for built-ins (e.g. <c>12bar_blues</c>), GUID for user progressions.</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name (e.g. <c>12-Bar Blues</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Canonical Nashville DSL — the v1 serialization (e.g. <c>17 17 17 17 47 47 17 17 57 47 17 57</c>).</summary>
    public string Dsl { get; set; } = "";

    /// <summary>Built-in vs user-defined (stored as its name).</summary>
    public ProgressionOrigin Origin { get; set; }

    /// <summary>When this definition was first saved (UTC).</summary>
    public DateTime CreatedUtc { get; set; }
}

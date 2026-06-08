namespace ChordFlow.Domain;

/// <summary>
/// A chord progression expressed as key-independent roman-numeral degrees
/// (e.g. 12-bar blues), realized into concrete chords by the <see cref="Transposer"/>.
/// </summary>
public sealed record Progression(string Id, string Name, IReadOnlyList<RomanDegree> Degrees);

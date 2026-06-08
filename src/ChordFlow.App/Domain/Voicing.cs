namespace ChordFlow.Domain;

/// <summary>How a chord is fingered: the set of fretted positions to strike together.</summary>
public sealed record Voicing(IReadOnlyList<FretPosition> Positions);

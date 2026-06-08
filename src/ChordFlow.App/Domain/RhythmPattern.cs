namespace ChordFlow.Domain;

/// <summary>Where chord hits and rests land within a single bar.</summary>
public sealed record RhythmPattern(string Id, string Name, IReadOnlyList<Beat> Beats);

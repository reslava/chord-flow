namespace ChordFlow.Music.Harmony;

/// <summary>A concrete chord: a root pitch class with a quality.</summary>
public sealed record Chord(PitchClass Root, Quality Quality);

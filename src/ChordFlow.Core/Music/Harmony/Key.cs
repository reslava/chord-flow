namespace ChordFlow.Music.Harmony;

/// <summary>A musical key: a tonic pitch class plus major/minor mode (e.g. Bb major).</summary>
public sealed record Key(PitchClass Tonic, bool IsMinor);

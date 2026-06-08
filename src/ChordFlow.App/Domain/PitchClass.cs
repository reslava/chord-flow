namespace ChordFlow.Domain;

/// <summary>
/// A pitch class 0..11 (0 = C, 1 = C#/Db, ... 11 = B), independent of octave.
/// Spelling (sharp vs flat) is resolved later for the active key.
/// </summary>
public readonly record struct PitchClass(int Value);

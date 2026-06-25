namespace ChordFlow.Music.Harmony;

/// <summary>
/// A concrete chord: a root pitch class with a quality. <see cref="RootSpelling"/>, when present,
/// is the letter-pure name the transposer derived from the written degree (e.g. <c>B</c> for <c>#4</c>
/// in F) and overrides the key-table spelling at display time; <c>null</c> means "spell from the key".
/// </summary>
public sealed record Chord(PitchClass Root, Quality Quality, NoteName? RootSpelling = null);

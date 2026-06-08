namespace ChordFlow.Domain;

/// <summary>
/// One beat within a bar: its duration and whether the chord is struck
/// (<c>IsHit == true</c>) or it is a rest (<c>IsHit == false</c>).
/// </summary>
public readonly record struct Beat(Duration Duration, bool IsHit);

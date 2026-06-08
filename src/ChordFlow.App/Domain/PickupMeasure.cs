namespace ChordFlow.Domain;

/// <summary>
/// A pickup / anacrusis modeled as its own short <b>leading measure</b> with its own tick length
/// (ctx IN11) — never a negative position, which would complicate bar math and rendering. Quantizes
/// exactly like a full bar, just with a shorter <paramref name="LengthTicks"/>.
/// </summary>
public sealed record PickupMeasure(IReadOnlyList<RhythmEvent> Events, int LengthTicks);

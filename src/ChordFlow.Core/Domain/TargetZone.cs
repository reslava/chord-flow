namespace ChordFlow.Domain;

/// <summary>
/// A lead-training "sweet spot": a chord-relative <see cref="ChordTone"/> to aim for, with an
/// <see cref="Importance"/> (ctx IN14). Chord-relative so it transposes with the chord — resolved to
/// concrete pitch classes / fretboard positions late (by <see cref="LeadTargets"/>). Domain only —
/// no fretboard UI here (ctx EX5).
/// </summary>
public readonly record struct TargetZone(ChordTone Tone, Importance Importance);

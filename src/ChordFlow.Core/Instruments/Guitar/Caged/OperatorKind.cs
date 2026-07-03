namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The kind of transform a <see cref="IVoicingOperator"/> performs (voicings-engine-rules-reference §1) — the
/// taxonomy that keeps the operator library honest instead of forcing every family through one "filter" metaphor:
/// <list type="bullet">
/// <item><see cref="DeriveFromFormula"/> — build a grip directly from the quality formula + fretboard geometry
///   (CAGED, Shell).</item>
/// <item><see cref="Reduce"/> — take an existing grip and mute notes by chord-tone function (Doubled-shell).</item>
/// <item><see cref="Revoice"/> — rearrange voices by octave displacement (future: Drop2/Drop3).</item>
/// <item><see cref="Augment"/> — change the chord-tone set before voicing (future: 6/9, add9, sus).</item>
/// </list>
/// Only <see cref="DeriveFromFormula"/> and <see cref="Reduce"/> are instantiated today; the other two are named
/// so the taxonomy is complete and a new family declares where it belongs.
/// </summary>
public enum OperatorKind
{
    DeriveFromFormula,
    Reduce,
    Revoice,
    Augment,
}

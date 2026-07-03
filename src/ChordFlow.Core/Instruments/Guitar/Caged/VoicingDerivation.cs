using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// One selected chord tone of the <b>abstract</b> voicing — the instrument-agnostic half of a
/// <see cref="VoicingDerivation"/> (voicings-engine-rules-reference §2). It is <i>which</i> chord tones the family
/// includes, by function, read from <see cref="ChordTones"/> — no fretboard geometry. A family selects a subset:
/// CAGED = the full chord; doubled-shell = the chord minus the Fifth; shell = root + Third + (Seventh|Sixth).
/// <paramref name="Interval"/> is semitones from the root; resolve to a concrete pitch class only when a root is
/// supplied (spelling stays deferred).
/// </summary>
public sealed record ToneSelection(int Interval, ChordToneFunction Function)
{
    /// <summary>The concrete pitch class of this tone when the chord root is <paramref name="root"/>.</summary>
    public PitchClass PitchClassFor(PitchClass root) => new(((root.Value + Interval) % 12 + 12) % 12);
}

/// <summary>
/// The first-class result of a <see cref="IVoicingOperator"/> — the introspectable <b>derivation trace</b>, not
/// just a grip (voicings-engine design §2.2). It carries the abstract voicing (<see cref="ToneSelection"/>), the
/// ordered "show your work" (<see cref="Realization"/>), and the realized <see cref="Grip"/>. <see cref="Grip"/> is
/// the same <see cref="ChordShape"/> the pre-existing consumers already read — the backward-compatible field the
/// <c>FamilyVoicing.Derive</c> grip shim returns, so nothing downstream changes.
/// </summary>
/// <param name="Family">The voicing family this derivation produced.</param>
/// <param name="Kind">The operator kind that produced it.</param>
/// <param name="Params">The resolved parameter knobs (echoed for display + the synthetic id).</param>
/// <param name="ToneSelection">The abstract voicing — the selected chord tones by function.</param>
/// <param name="Realization">The ordered geometry steps that placed the tones on the neck.</param>
/// <param name="Grip">The realized grip — the only field the legacy consumers read.</param>
public sealed record VoicingDerivation(
    VoicingFamily Family,
    OperatorKind Kind,
    IReadOnlyList<ResolvedParam> Params,
    IReadOnlyList<ToneSelection> ToneSelection,
    IReadOnlyList<RealizationStep> Realization,
    ChordShape Grip);

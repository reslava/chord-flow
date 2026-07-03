namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// A named, introspectable voicing-derivation operator — the unit of the guitar Voicings Engine
/// (voicings-engine-rules-reference §1, design §2.1). Each family (CAGED, shell, doubled-shell, and future ones)
/// is one operator: it declares its <see cref="Kind"/> and its typed <see cref="Parameters"/>, and
/// <see cref="Derive"/> turns a <see cref="VoicingRequest"/> into a first-class <see cref="VoicingDerivation"/>
/// (the abstract tone selection + the "show your work" realization + the grip).
/// <para>
/// Guitar-scoped by design: the operator lives in <c>ChordFlow.Instruments.Guitar</c>. A cross-instrument core
/// (<c>IVoicingsE</c>) is deferred until a second instrument exists — this interface makes the guitar engine
/// introspectable without guessing that seam.
/// </para>
/// </summary>
public interface IVoicingOperator
{
    /// <summary>The voicing family this operator produces (its stable identity + id token).</summary>
    VoicingFamily Family { get; }

    /// <summary>The kind of transform it performs.</summary>
    OperatorKind Kind { get; }

    /// <summary>A human display name, e.g. "CAGED (full chord)".</summary>
    string DisplayName { get; }

    /// <summary>The operator's declared, typed parameter surface (what a UI renders + what a request must supply).</summary>
    ParameterSchema Parameters { get; }

    /// <summary>Derive the voicing for <paramref name="request"/>. Validates against <see cref="Parameters"/> and
    /// throws (no anchor / unspellable / bad parameter) when no clean grip exists — that is the caller's region filter.</summary>
    VoicingDerivation Derive(VoicingRequest request);
}

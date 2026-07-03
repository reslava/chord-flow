namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The kind of a <see cref="RealizationStep"/> — the geometry decisions a derivation makes, named so the inspector
/// page can style each and tests can assert the narration structurally (not just its prose).
/// </summary>
public enum RealizationStepKind
{
    /// <summary>Placed the shape's root anchor(s) + octave zone in the region.</summary>
    Anchor,

    /// <summary>Picked the bass (lowest-pitch) root string + fret the grip is built from.</summary>
    BassRoot,

    /// <summary>Bounded the reach window the box may extend into (incl. any stretch-back).</summary>
    ReachWindow,

    /// <summary>Muted string(s) — below the bass root, or by chord-tone function.</summary>
    Mute,

    /// <summary>Selected one chord tone per played string.</summary>
    Select,

    /// <summary>Derived the anchor finger from the root's rank in the realized box.</summary>
    AnchorFinger,

    /// <summary>Chose the guide-tone placements (shell forms).</summary>
    GuideTones,

    /// <summary>Anchored at the lowest compact placement (shell), pushing an open root up an octave if needed.</summary>
    Compaction,

    /// <summary>Reduced an upstream grip by muting a chord-tone function (doubled-shell).</summary>
    Reduce,
}

/// <summary>
/// One ordered "show your work" step of a <see cref="VoicingDerivation"/> — the explainable trace of how the
/// abstract tone selection was realized on the neck (voicings-engine design OD-1: structured, not prose-only).
/// <paramref name="Label"/> is the rendered human sentence; <paramref name="Kind"/> classifies it; the optional
/// <paramref name="Strings"/> are the guitar strings the step concerns (e.g. the strings muted, or the played
/// strings a selection covers) so tests can check the narration against the grip.
/// </summary>
public sealed record RealizationStep(
    RealizationStepKind Kind,
    string Label,
    IReadOnlyList<int>? Strings = null);

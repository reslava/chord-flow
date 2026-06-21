using ChordFlow.Music.Melody;
using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The concrete, first-class <b>guitar adapter</b> — a deliberate public surface over the guitar
/// realization pieces (<see cref="VoicingBook"/>, <see cref="Fretboard"/>, <see cref="VoicingDiagram"/>).
/// It realizes a chord to a playable fret <see cref="Voicing"/>, produces the spatial
/// <see cref="FretboardDiagram"/>, and resolves a lead <see cref="TargetZone"/> to fret positions —
/// the guitar-specific operations that used to be scattered across <c>Domain/</c>. Pure: no I/O; the
/// authored library is supplied via the injected <see cref="VoicingBook"/> (built at the feature seam).
/// </summary>
/// <remarks>
/// This is the surface the deferred polymorphic <c>IInstrument</c> is later extracted from (its first
/// real caller appears in the <c>guitar/instrument-rendering</c> thread) — not built speculatively now.
/// <para>
/// <b>Authored↔CAGED-derived reconciliation is intentionally out of scope here</b> (owned by the
/// <c>guitar/caged-system</c> thread). Authored voicings are the golden oracle the future derivation
/// engine is validated against, and the runtime extension point is <see cref="VoicingBook"/>'s shadow
/// rule (stored authored shadows generated): the derived source slots in there additively, without
/// changing this facade or its signatures.
/// </para>
/// </remarks>
public sealed class GuitarInstrument
{
    private readonly VoicingBook _voicings;

    /// <summary>Create the adapter over a built <see cref="VoicingBook"/> (authored library + strategy fallback).</summary>
    public GuitarInstrument(VoicingBook voicings)
    {
        ArgumentNullException.ThrowIfNull(voicings);
        _voicings = voicings;
    }

    /// <summary>
    /// The playable guitar <see cref="Voicing"/> for <paramref name="chord"/> at <paramref name="difficulty"/>:
    /// the top authored voicing if one exists, else the strategy-generated shape — delegates
    /// <see cref="VoicingBook.Lookup"/>. The returned voicing carries its <see cref="FretPosition"/>s.
    /// </summary>
    public Voicing Realize(Chord chord, Difficulty difficulty) => _voicings.Lookup(chord, difficulty);

    /// <summary>
    /// The spatial <see cref="FretboardDiagram"/> for an authored <paramref name="shape"/>, at its
    /// canonical-C anchor — delegates <see cref="VoicingDiagram.Build"/>. Shape-based because the diagram
    /// is anchor-fixed today (the root-picker is deferred); a chord-keyed overload arrives with root-aware
    /// diagrams in a later thread.
    /// </summary>
    public FretboardDiagram Diagram(VoicingShape shape) => VoicingDiagram.Build(shape);

    /// <summary>
    /// Every fretboard <see cref="FretPosition"/> (0..<paramref name="maxFret"/>) that sounds the lead
    /// target <paramref name="zone"/> over <paramref name="chord"/>. The guitar realization of a pure
    /// <see cref="LeadTargets"/> pitch class — relocated here so <c>Domain</c> stays instrument-agnostic.
    /// </summary>
    public IReadOnlyList<FretPosition> ResolveLead(Chord chord, TargetZone zone, int maxFret = Fretboard.DefaultMaxFret) =>
        Fretboard.PositionsFor(LeadTargets.PitchClassOf(chord, zone), maxFret);
}

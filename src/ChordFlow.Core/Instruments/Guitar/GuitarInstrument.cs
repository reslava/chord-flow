using ChordFlow.Music.Melody;
using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The concrete, first-class <b>guitar adapter</b> — a deliberate public surface over the guitar realization
/// pieces (<see cref="VoicingDiagram"/>, <see cref="Fretboard"/>, <see cref="LeadTargets"/>): it builds the
/// spatial <see cref="FretboardDiagram"/> for a shape and resolves a lead <see cref="TargetZone"/> to fret
/// positions — the guitar-specific operations that used to be scattered across <c>Domain/</c>. Pure: no I/O.
/// </summary>
/// <remarks>
/// This is the surface the deferred polymorphic <c>IInstrument</c> is later extracted from — not built
/// speculatively now. Per-chord <b>voicing selection</b> is no longer an instrument concern: it moved to the
/// Features comping resolver (engine-derived-as-app-source D4=(B)), which derives engine grips directly — so the
/// old <c>VoicingBook</c>/strategy realization this facade once delegated to has been retired
/// (shell-voicing-derivation IN9).
/// </remarks>
public sealed class GuitarInstrument
{
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

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Rendering;

/// <summary>
/// One entry in a score's <b>chord schedule</b>: the chord that becomes active at a given beat, with a fretboard
/// diagram of the comped voicing the tab actually plays for it. Emitted once per chord change as a by-product of
/// the render pass (so the diagram is exactly what sounds — no parallel re-derivation, design D1 / req C2), and
/// consumed by the now/next fretboards over the bridge.
/// </summary>
/// <param name="Bar">0-based master-bar index (lines up with alphaTab's <c>beat.voice.bar.index</c>).</param>
/// <param name="Beat">0-based beat ordinal within the bar (lines up with alphaTab's <c>beat.index</c>).</param>
/// <param name="Name">Chord symbol spelled against the bar's key — the fretboard title.</param>
/// <param name="Diagram">Real-root fretboard diagram of the comped voicing (<see cref="RealizedVoicingDiagram"/>).</param>
public sealed record ChordChange(int Bar, int Beat, string Name, FretboardDiagram Diagram);

/// <summary>
/// The full output of a score render: the alphaTex string plus the <see cref="Schedule"/> (one
/// <see cref="ChordChange"/> per chord change) the now/next fretboards drive off. The two are produced in one
/// render pass so they cannot drift from each other (design D1, rejecting a parallel schedule builder).
/// </summary>
public sealed record RenderResult(string Tex, IReadOnlyList<ChordChange> Schedule);

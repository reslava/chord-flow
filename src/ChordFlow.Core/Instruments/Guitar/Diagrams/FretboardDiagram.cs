using ChordFlow.Domain;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The <b>layer</b> channel of a <see cref="FretboardMarker"/> — which overlaid entity a dot belongs to, so
/// several can share one diagram (a chord-tone <c>Circle</c> over a scale <c>Square</c>, the root as a
/// <c>Ring</c>, a guide/target tone as a <c>Diamond</c>). Independent of color, which encodes the interval.
/// Crosses the C#→JS bridge as its integer ordinal; the JS view maps it by index.
/// </summary>
public enum MarkerShape
{
    Circle,
    Square,
    Diamond,
    Ring,
}

/// <summary>
/// One drawn position on a <see cref="FretboardDiagram"/> — the general spatial unit a voicing, scale,
/// arpeggio, or interval-lattice producer emits. Unlike the old per-string voicing model, <b>many markers may
/// share a string</b> (a scale has several notes per string); that generalization is what makes the view
/// reusable (req <c>IN2</c>). All music theory is resolved in Core (<c>C1</c>): the marker carries both the
/// spelled <see cref="Note"/> and the <see cref="Interval"/> label so the view's label toggle needs no re-fetch.
/// </summary>
/// <param name="String">alphaTab string numbering: 1 = high E .. 6 = low E (matches <see cref="FretPosition"/>).</param>
/// <param name="Fret">Absolute fret; <c>0</c> = an open-string marker (drawn as a ringed dot above the nut).</param>
/// <param name="Note">Spelled note name (e.g. <c>Eb</c>) — shown in "note" label mode.</param>
/// <param name="Interval">Interval label against the entity's root (<c>R</c>/<c>b3</c>/<c>5</c>/<c>b7</c>/<c>#5</c>/<c>bb7</c>…) — shown in "interval" label mode, and the key for an override per-interval palette.</param>
/// <param name="Function">The <b>color</b> channel — the function color-key the default palette maps to: <c>root</c>/<c>third</c>/<c>fifth</c>/<c>seventh</c>/<c>tension</c>. A string (not the chord-tone enum) because <c>tension</c> is not a chord-tone function and the bridge serializes the key by name.</param>
/// <param name="Shape">The <b>layer</b> channel (see <see cref="MarkerShape"/>).</param>
public sealed record FretboardMarker(
    int String,
    int Fret,
    string Note,
    string Interval,
    string Function,
    MarkerShape Shape);

/// <summary>
/// The general Core-computed carrier the <c>ChordFlowFretboard</c> JS view draws — the spatial twin of the
/// alphaTex string the <c>ChordFlowScore</c> view draws. A flat <see cref="Markers"/> list plus diagram-level
/// chrome. Voicings are one producer (<see cref="VoicingDiagram.Build"/>); scales/arpeggios/intervals attach
/// additively as their domain ships. Theory lives here; the JS is a dumb drawer (<c>IN6</c>/<c>C1</c>).
/// </summary>
/// <param name="Title">Display title (e.g. the chord symbol <c>Cmaj7</c>).</param>
/// <param name="Markers">Every note to draw — open strings are markers at <c>Fret = 0</c>.</param>
/// <param name="MutedStrings">Diagram-level chrome: strings the player should not sound (drawn <c>✕</c>). A voicing fills this; a scale view leaves it empty. Distinct from "a note to draw".</param>
/// <param name="BarreFret">Fret of a barre across strings, if any.</param>
/// <param name="FretMin">Lowest fret of the window; the view auto-fits to the markers when null.</param>
/// <param name="FretMax">Highest fret of the window; the view auto-fits to the markers when null.</param>
public sealed record FretboardDiagram(
    string Title,
    IReadOnlyList<FretboardMarker> Markers,
    IReadOnlyList<int> MutedStrings,
    int? BarreFret,
    int? FretMin,
    int? FretMax);

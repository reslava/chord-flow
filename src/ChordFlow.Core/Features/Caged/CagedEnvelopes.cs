using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Features.Caged;

/// <summary>
/// Outbound bridge envelopes (C#→JS) for the CAGED Shapes page. The page sends a <c>cagedPreview</c> request
/// (a CAGED shape + a root) and gets back a <see cref="FretboardDiagram"/> marker model the shared
/// <c>ChordFlowFretboard</c> view draws — the same carrier the voicing/scale previews use, so the JS needs no new
/// model. An unknown shape comes back as a <see cref="CagedErrorEnvelope"/> shown inline. Mirrors the Scales slice.
/// </summary>

/// <summary>A built CAGED octave-shape diagram: <c>{"type":"cagedDiagram","diagram":{…FretboardDiagram…}}</c>.</summary>
public sealed record CagedDiagramEnvelope(FretboardDiagram Diagram, string Type = "cagedDiagram");

/// <summary>An unknown CAGED shape: the message shown inline. <c>{"type":"cagedError","message":"…"}</c>.</summary>
public sealed record CagedErrorEnvelope(string Message, string Type = "cagedError");

/// <summary>A derived CAGED chord diagram (the CAGED Chords page): <c>{"type":"cagedChordDiagram","diagram":{…FretboardDiagram…}}</c>.</summary>
public sealed record CagedChordDiagramEnvelope(FretboardDiagram Diagram, string Type = "cagedChordDiagram");

/// <summary>An unknown quality/shape or an unvoiceable combo: shown inline. <c>{"type":"cagedChordError","message":"…"}</c>.</summary>
public sealed record CagedChordErrorEnvelope(string Message, string Type = "cagedChordError");

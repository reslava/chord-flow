using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Features.Scales;

/// <summary>
/// Outbound bridge envelopes (C#→JS) for the Scales page. The page sends a <c>scalePreview</c> request
/// (an interval set + a root) and gets back a <see cref="FretboardDiagram"/> marker model the shared
/// <c>ChordFlowFretboard</c> view draws — the same carrier the voicing preview uses, so the JS needs no new
/// model. A bad interval token comes back as a <see cref="ScaleErrorEnvelope"/> shown inline.
/// </summary>

/// <summary>A built interval-set diagram: <c>{"type":"scaleDiagram","diagram":{…FretboardDiagram…}}</c>.</summary>
public sealed record ScaleDiagramEnvelope(FretboardDiagram Diagram, string Type = "scaleDiagram");

/// <summary>An unparseable interval set: the located message, shown inline. <c>{"type":"scaleError","message":"…"}</c>.</summary>
public sealed record ScaleErrorEnvelope(string Message, string Type = "scaleError");

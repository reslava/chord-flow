using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// Outbound bridge envelopes (C#→JS) for GuitarVoicingsR — the faceted voicings grid. The component sends a
/// <c>voicingGrid</c> request (the filter state) and gets back a <see cref="VoicingGridResultEnvelope"/>: the whole
/// filtered grid resolved in one round-trip (C4), each cell a realized <see cref="FretboardDiagram"/> the shared
/// FretR view draws. An empty filter result is an empty <see cref="VoicingGridResultEnvelope.Cells"/> list, never an
/// error (C5). Mirrors the CAGED Chords slice's envelope shape; the JS needs no new diagram model.
/// </summary>

/// <summary>
/// One realized voicing cell of the grid: the synthetic <paramref name="Id"/> (<c>auto:shell:dom7:E</c> …) shown
/// with copy-to-clipboard, a display <paramref name="Title"/> (e.g. "Dominant 7 (shell) — E shape"), the
/// <paramref name="Family"/>/<paramref name="Quality"/>/<paramref name="Shape"/> grouping keys (rows by quality,
/// columns by shape/form — IN5), and the realized <paramref name="Diagram"/>.
/// </summary>
public sealed record VoicingGridCell(
    string Id, string Title, string Family, string Quality, string Shape, FretboardDiagram Diagram);

/// <summary>The filtered grid: <c>{"type":"voicingGridResult","cells":[{…VoicingGridCell…}]}</c> — ordered rows-by-quality then cols-by-shape.</summary>
public sealed record VoicingGridResultEnvelope(IReadOnlyList<VoicingGridCell> Cells, string Type = "voicingGridResult");

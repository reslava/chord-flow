using ChordFlow.Instruments.Drums;

namespace ChordFlow.Features.Drums;

/// <summary>
/// Reply to the <c>drumPreview</c> verb: the rendered alphaTex percussion track (for ScoreR notation +
/// playback) and the <see cref="DrumGrooveDiagram"/> grid model (for DrumsR). Both are projections of one
/// parse of the same groove DSL, so the score, the grid, and the playback marker cannot drift.
/// </summary>
public sealed record DrumPreviewEnvelope(string Tex, DrumGrooveDiagram Diagram, string Type = "drumPreview");

/// <summary>A fail-loud parse error for the <c>drumPreview</c> verb (shown inline on the Drums page).</summary>
public sealed record DrumPreviewErrorEnvelope(string Message, string Type = "drumPreviewError");

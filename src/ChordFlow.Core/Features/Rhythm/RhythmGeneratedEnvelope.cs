using ChordFlow.Instruments.Drums;

namespace ChordFlow.Features.Rhythm;

/// <summary>
/// Reply to the <c>rhythmGenerate</c> verb: the rendered alphaTex percussion track (<see cref="Tex"/>, for
/// ScoreR notation + playback), the <see cref="DrumGrooveDiagram"/> grid model (for DrumsR), and a plain
/// onset-ASCII <see cref="Grid"/> debug string (x = attack, . = rest cell; beats space-separated, bars
/// <c>|</c>). All three are projections of one generated onset grid, so the notation, the grid, and the
/// playback marker cannot drift (mirrors the <c>drumPreview</c> reply).
/// </summary>
public sealed record RhythmGeneratedEnvelope(
    string Tex, DrumGrooveDiagram Diagram, string Grid, string Type = "rhythmGenerated");

/// <summary>A fail-loud error for the <c>rhythmGenerate</c> verb (shown inline on the page).</summary>
public sealed record RhythmGenerateErrorEnvelope(string Message, string Type = "rhythmGenerateError");

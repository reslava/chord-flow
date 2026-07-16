namespace ChordFlow.Features.ChordSheets;

/// <summary>
/// Outbound bridge envelope (C#→JS) for the chord-sheet PDF export flow. The sheet MODEL no longer has its own
/// request/reply pair: since harmony-controls-r (IN3) the sheet + cellSchedule ride the unified
/// <c>loadScore</c> reply (see <c>LoadScoreEnvelope</c>) as projections of the one Exercise render pass —
/// the <c>chordSheet</c> verb and its <c>chordSheetResult</c>/<c>chordSheetError</c> envelopes are retired.
/// Only the print round-trip remains here.
/// </summary>
/// The PDF-export outcome (so the page can tear down its print container): <c>Ok</c> true with the written
/// <c>Path</c>, or false with an optional <c>Message</c> (a cancel is <c>Ok=false</c> with no message).
/// </summary>
public sealed record ChordSheetPdfDoneEnvelope(bool Ok, string? Path = null, string? Message = null, string Type = "chordSheetPdfDone");

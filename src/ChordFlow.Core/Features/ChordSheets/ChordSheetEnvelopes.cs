using ChordFlow.Rendering.ChordSheets;

namespace ChordFlow.Features.ChordSheets;

/// <summary>
/// Outbound bridge envelopes (C#→JS) for ChordSheetR. The page sends a <c>chordSheet</c> request (harmony ref +
/// key/adornment) and gets back a <see cref="ChordSheetResultEnvelope"/> carrying the whole
/// <see cref="ChordSheet"/> model the JS view draws, or a <see cref="ChordSheetErrorEnvelope"/> (UI-safe
/// fail-loud, like <c>voicingDeriveError</c>) when the harmony can't be resolved.
/// </summary>

/// <summary>
/// The built chord sheet plus its playback payload:
/// <c>{"type":"chordSheetResult","sheet":{…ChordSheet…},"cellSchedule":[…],"tex":"…"}</c>. <see cref="CellSchedule"/>
/// maps score (bar,beat) → the sounding cell/chord for the animated marker; <see cref="Tex"/> is the playable
/// alphaTex the page's own ChordFlowPlayback engine renders + plays (both derived from one realized-song pass so
/// they align — design D1-a).
/// </summary>
public sealed record ChordSheetResultEnvelope(
    ChordSheet Sheet,
    IReadOnlyList<CellScheduleEntry> CellSchedule,
    string Tex,
    string Type = "chordSheetResult");

/// <summary>A UI-safe failure (missing harmony, bad reference): <c>{"type":"chordSheetError","message":"…"}</c>.</summary>
public sealed record ChordSheetErrorEnvelope(string Message, string Type = "chordSheetError");

/// <summary>
/// The PDF-export outcome (so the page can tear down its print container): <c>Ok</c> true with the written
/// <c>Path</c>, or false with an optional <c>Message</c> (a cancel is <c>Ok=false</c> with no message).
/// </summary>
public sealed record ChordSheetPdfDoneEnvelope(bool Ok, string? Path = null, string? Message = null, string Type = "chordSheetPdfDone");

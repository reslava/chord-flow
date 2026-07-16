using ChordFlow.Rendering;
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
/// <c>{"type":"chordSheetResult","sheet":{…ChordSheet…},"cellSchedule":[…],"chordSchedule":[…],"tex":"…"}</c>.
/// <see cref="CellSchedule"/> maps score (bar,beat) → the sounding cell/chord for the animated marker;
/// <see cref="ChordSchedule"/> is the now/next-fretboards feed (one <see cref="ChordChange"/> per chord change,
/// each with the comped voicing's diagram — the SAME wire shape Practice's <c>loadScore</c> carries); and
/// <see cref="Tex"/> is the playable alphaTex the page's own ChordFlowPlayback engine renders + plays. All three
/// are separate projections of ONE realized-song render pass (design D1-a), so they cannot drift; the schedule is
/// surfaced from <c>RenderResult.Schedule</c> the handler already computes (no separate producer).
/// </summary>
public sealed record ChordSheetResultEnvelope(
    ChordSheet Sheet,
    IReadOnlyList<CellScheduleEntry> CellSchedule,
    IReadOnlyList<ChordChange> ChordSchedule,
    string Tex,
    string Type = "chordSheetResult");

/// <summary>A UI-safe failure (missing harmony, bad reference): <c>{"type":"chordSheetError","message":"…"}</c>.</summary>
public sealed record ChordSheetErrorEnvelope(string Message, string Type = "chordSheetError");

/// <summary>
/// The PDF-export outcome (so the page can tear down its print container): <c>Ok</c> true with the written
/// <c>Path</c>, or false with an optional <c>Message</c> (a cancel is <c>Ok=false</c> with no message).
/// </summary>
public sealed record ChordSheetPdfDoneEnvelope(bool Ok, string? Path = null, string? Message = null, string Type = "chordSheetPdfDone");

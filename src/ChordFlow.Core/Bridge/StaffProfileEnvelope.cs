namespace ChordFlow.Bridge;

/// <summary>
/// Outbound reply (C#→JS) to a <c>getStaffProfile</c> request: the persisted staff-display profile the score
/// view should apply on boot. Serializes to <c>{"type":"staffProfile","profile":"tab|standard|both"}</c>.
/// The inbound siblings (<c>getStaffProfile</c> / <c>setStaffProfile</c>) carry no dedicated envelope — they
/// are parsed by <see cref="WebMessageRouter"/>. The profile is a display-only choice (which staves alphaTab
/// shows); no alphaTex / render change reaches Core (constraint C6) — the score-render component flips the
/// per-staff <c>showStandardNotation</c>/<c>showTablature</c> flags locally.
/// </summary>
public sealed record StaffProfileEnvelope(string Profile, string Type = "staffProfile");

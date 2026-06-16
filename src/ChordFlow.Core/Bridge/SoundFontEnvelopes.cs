namespace ChordFlow.Bridge;

/// <summary>One picker row: the soundfont <see cref="Id"/> (file name) and its display <see cref="Name"/>.</summary>
public sealed record SoundFontDto(string Id, string Name);

/// <summary>
/// Outbound reply (C#→JS) to a <c>listSoundFonts</c> request: the available fonts plus the id the UI should
/// load/select. Serializes to <c>{"type":"soundFontsListed","fonts":[{id,name}],"selectedId":"…"}</c>.
/// The inbound siblings (<c>listSoundFonts</c> / <c>setSoundFont</c>) carry no dedicated envelope — they are
/// parsed by <see cref="WebMessageRouter"/>. Font is not a render input, so there is no <c>renderOptions</c>
/// change (constraint C4).
/// </summary>
public sealed record SoundFontsListedEnvelope(
    IReadOnlyList<SoundFontDto> Fonts, string SelectedId, string Type = "soundFontsListed");

namespace ChordFlow.Bridge;

/// <summary>One available soundfont: the <see cref="Id"/> (its file name, e.g. <c>sonivox.sf2</c>)
/// and a human-friendly <see cref="DisplayName"/> for the picker.</summary>
public sealed record SoundFontInfo(string Id, string DisplayName);

/// <summary>
/// Lists the soundfonts available for playback. The seam lives in Core so the engine stays UI/host-agnostic
/// (constraint C2); the desktop host implements it by scanning its served <c>wwwroot/soundfont</c> folder, and
/// a future web host plugs in its own catalog. Auto-discovery is the contract — adding a font is a data drop,
/// not a code change (IN2).
/// </summary>
public interface ISoundFontCatalog
{
    /// <summary>The available soundfonts, by id. May be empty (the picker then falls back to the default).</summary>
    IReadOnlyList<SoundFontInfo> List();
}

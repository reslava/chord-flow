using System.Globalization;
using ChordFlow.Bridge;

namespace ChordFlow.Desktop.WebHost;

/// <summary>
/// Desktop <see cref="ISoundFontCatalog"/>: lists the <c>*.sf2</c> / <c>*.sf3</c> files served from the host's
/// <c>wwwroot/soundfont</c> folder. Auto-discovery means dropping a new font in the folder makes it appear in
/// the picker with no code change (IN2). The id is the file name (what the WebView appends to the
/// <c>soundfont/</c> URL); the display name is derived from it.
/// </summary>
public sealed class WwwrootSoundFontCatalog : ISoundFontCatalog
{
    // alphaTab loads SoundFont2 (.sf2) and its Ogg-compressed variant (.sf3) interchangeably.
    private static readonly HashSet<string> SoundFontExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".sf2", ".sf3" };

    private readonly string _folder;

    public WwwrootSoundFontCatalog(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        _folder = folder;
    }

    /// <inheritdoc/>
    public IReadOnlyList<SoundFontInfo> List()
    {
        if (!Directory.Exists(_folder))
        {
            return Array.Empty<SoundFontInfo>();
        }

        return Directory.EnumerateFiles(_folder)
            .Where(path => SoundFontExtensions.Contains(Path.GetExtension(path)))
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new SoundFontInfo(name!, FriendlyName(name!)))
            .ToList();
    }

    // "fluidr3_gm.sf2" → "Fluidr3 Gm". Strip the extension, split on separators, title-case each word.
    private static string FriendlyName(string fileName)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName)
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();
        if (stem.Length == 0)
        {
            return fileName;
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(stem);
    }
}

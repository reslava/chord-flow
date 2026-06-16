using ChordFlow.Bridge;
using ChordFlow.Persistence;

namespace ChordFlow.Features;

/// <summary>
/// The playback-soundfont feature slice: composes the host's <see cref="ISoundFontCatalog"/> (which fonts
/// exist) with <see cref="IAppSettings"/> (the persisted global choice) into the data the picker needs, and
/// persists a new selection. No alphaTex / Domain change — this only chooses which synth soundfont plays
/// (constraint C1). The choice is a single app-wide setting, stored under <see cref="SelectedKey"/> (IN3).
/// </summary>
public sealed class SoundFontLibrary
{
    /// <summary>The <see cref="IAppSettings"/> key the selected soundfont id is stored under.</summary>
    public const string SelectedKey = "playback.soundFont";

    /// <summary>The default/fallback soundfont id — the one font that ships in the repo (IN5).</summary>
    public const string DefaultFont = "sonivox.sf2";

    private readonly ISoundFontCatalog _catalog;
    private readonly IAppSettings _settings;

    public SoundFontLibrary(ISoundFontCatalog catalog, IAppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(settings);
        _catalog = catalog;
        _settings = settings;
    }

    /// <summary>
    /// The available fonts plus the id the UI should load. The selected id is the persisted choice when it
    /// still names an available font; otherwise it falls back to the default when present, else the first
    /// available font, else the default id (so the field is never empty even on an empty catalog).
    /// </summary>
    public SoundFontsListedEnvelope ListWithSelection()
    {
        IReadOnlyList<SoundFontInfo> fonts = _catalog.List();
        var dtos = fonts.Select(f => new SoundFontDto(f.Id, f.DisplayName)).ToList();

        bool Exists(string? id) => id is not null && fonts.Any(f => f.Id == id);

        string? stored = _settings.Get(SelectedKey);
        string selected =
            Exists(stored) ? stored! :
            Exists(DefaultFont) ? DefaultFont :
            fonts.Count > 0 ? fonts[0].Id :
            DefaultFont;

        return new SoundFontsListedEnvelope(dtos, selected);
    }

    /// <summary>Persist a new global soundfont choice.</summary>
    public void SetSelected(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _settings.Set(SelectedKey, id);
    }
}

namespace ChordFlow.Features.Packs;

/// <summary>
/// The content kinds a pack can carry. A definition's kind is determined by <b>which per-kind folder
/// it sits in</b> (design §6.3) — never a per-file field — so one bundle can mix kinds (a real genre
/// pack ships progressions + songs + rhythms + voicings together). This is distinct from the manifest's
/// coarse <c>kind</c> (the pack-type discriminator: <c>content</c> vs. future <c>soundfont</c>/<c>theme</c>).
/// </summary>
public enum ContentKind
{
    Progression,
    Song,
    Rhythm,
    Voicing,
}

/// <summary>Folder-name mapping for <see cref="ContentKind"/> — the single place kinds and bundle subfolders line up.</summary>
public static class ContentKinds
{
    /// <summary>The bundle subfolder that holds definitions of this kind (e.g. <c>progressions</c>).</summary>
    public static string Folder(this ContentKind kind) => kind switch
    {
        ContentKind.Progression => "progressions",
        ContentKind.Song => "songs",
        ContentKind.Rhythm => "rhythms",
        ContentKind.Voicing => "voicings",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown content kind."),
    };

    /// <summary>
    /// Every content kind, in a stable enumeration order — progressions and rhythms before songs, so that
    /// when a pack is imported the definitions a song may reference already exist as rows. (Reference
    /// resolution is fail-loud at realize time regardless, but importing dependencies first keeps it tidy.)
    /// </summary>
    public static readonly IReadOnlyList<ContentKind> All = new[]
    {
        ContentKind.Progression,
        ContentKind.Rhythm,
        ContentKind.Voicing,
        ContentKind.Song,
    };
}

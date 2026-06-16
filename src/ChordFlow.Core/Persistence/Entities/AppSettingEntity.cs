namespace ChordFlow.Persistence.Entities;

/// <summary>
/// One global application setting as a key/value pair — the durable home for app-wide
/// preferences (e.g. the chosen playback soundfont) that aren't exercise content and so
/// belong in none of the four content stores. <see cref="Key"/> is the primary key; the
/// value is an opaque string the caller interprets.
/// </summary>
public sealed class AppSettingEntity
{
    /// <summary>The setting key (primary key), e.g. <c>"playback.soundFont"</c>.</summary>
    public string Key { get; set; } = "";

    /// <summary>The setting value as an opaque string.</summary>
    public string Value { get; set; } = "";
}

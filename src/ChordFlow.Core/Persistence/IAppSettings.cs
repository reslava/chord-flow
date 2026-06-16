namespace ChordFlow.Persistence;

/// <summary>
/// A tiny key/value accessor for global application settings — the seam features depend on
/// so a future host reuses the same Core-owned persistence (constraint C3). One key per
/// preference; values are opaque strings the caller interprets.
/// </summary>
public interface IAppSettings
{
    /// <summary>Read a setting value, or <c>null</c> when the key has never been set.</summary>
    string? Get(string key);

    /// <summary>Create or overwrite a setting value.</summary>
    void Set(string key, string value);
}

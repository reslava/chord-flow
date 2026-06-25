namespace ChordFlow.Persistence;

/// <summary>
/// Provenance of a stored content definition (progression, song, rhythm, voicing) — an <b>Entity-layer</b>
/// concern (constraint C1: never on a pure <c>Domain/</c> music-theory record). Two stored tiers:
/// <see cref="Pack"/> carries a non-null <c>PackId</c> naming its source pack (the default/starter pack
/// included — it is an ordinary package, id <c>"default"</c>), and <see cref="UserDefined"/> carries a null
/// PackId. The third content source — engine-derived <c>automatic</c> voicings — is computed, never stored,
/// so it has no <see cref="Origin"/>. (The former <c>BuiltIn</c> tier was retired when the default pack
/// became a package — see the content-source-model thread; a startup migration converts legacy rows.)
/// Tier/paywall enforcement is a separate Features/licensing concern (req EX4) — this only <i>records</i>
/// provenance.
/// </summary>
public enum Origin
{
    /// <summary>Created and saved locally by the user (GUID ids); null <c>PackId</c>.</summary>
    UserDefined,

    /// <summary>Imported from a content pack — the entity's <c>PackId</c> names which pack (the default pack uses id <c>"default"</c>).</summary>
    Pack,
}

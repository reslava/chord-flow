namespace ChordFlow.Persistence;

/// <summary>
/// Provenance of a stored content definition (progression, song, rhythm, voicing) — an <b>Entity-layer</b>
/// concern (constraint C1: never on a pure <c>Domain/</c> music-theory record). The persisted shape is this
/// discriminator plus an optional <c>PackId</c> column on the entity (design §2): <see cref="Pack"/> carries
/// a non-null pack id; <see cref="BuiltIn"/> and <see cref="UserDefined"/> carry null. Shared by every
/// content entity. Shadowing precedence is <c>UserDefined &gt; Pack &gt; BuiltIn</c> — resolved by the
/// Origin resolver, which ranks explicitly rather than relying on the declaration order here. Tier/paywall
/// enforcement is a separate Features/licensing concern (req EX4) — this only <i>records</i> origin.
/// </summary>
public enum Origin
{
    /// <summary>Ships in the default/starter pack; seeded on first run (stable human-slug ids, e.g. <c>12bar_blues</c>).</summary>
    BuiltIn,

    /// <summary>Created and saved locally by the user (GUID ids).</summary>
    UserDefined,

    /// <summary>Imported from a content pack — the entity's <c>PackId</c> names which pack.</summary>
    Pack,
}

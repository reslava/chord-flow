namespace ChordFlow.Domain;

/// <summary>
/// Where a stored progression came from. Recorded on every persisted progression (stored by name, like
/// <see cref="Difficulty"/>). <see cref="BuiltIn"/> ships with the app (stable human-slug ids, e.g.
/// <c>12bar_blues</c>); <see cref="UserDefined"/> is created by a user (GUID ids). Paywall/tier
/// enforcement is a separate Features/licensing concern (req EX4) — this only *records* origin.
/// </summary>
public enum ProgressionOrigin
{
    /// <summary>Ships with the app; seeded on first run.</summary>
    BuiltIn,

    /// <summary>Created and saved by a user.</summary>
    UserDefined,
}

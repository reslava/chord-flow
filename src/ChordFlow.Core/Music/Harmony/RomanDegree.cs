namespace ChordFlow.Music.Harmony;

/// <summary>
/// A chromatic alteration on a scale degree's root: <see cref="Sharp"/> (<c>#</c>) raises it a
/// semitone, <see cref="Flat"/> (<c>b</c>) lowers it, <see cref="Natural"/> leaves the diatonic
/// degree as-is. Combines with the degree's own diatonic accidental (e.g. in F major degree 4 is
/// B♭, so <c>#4</c> sounds B natural) and drives letter-pure root spelling.
/// </summary>
public enum Accidental
{
    Natural,
    Sharp,
    Flat,
}

/// <summary>
/// A key-independent scale degree (1 = I, 4 = IV, 5 = V, ...) carrying the chord
/// quality to use at that degree and an optional chromatic <see cref="Accidental"/>
/// (<c>#</c>/<c>b</c>) on its root.
/// </summary>
public readonly record struct RomanDegree(int Degree, Quality Quality, Accidental Accidental = Accidental.Natural);

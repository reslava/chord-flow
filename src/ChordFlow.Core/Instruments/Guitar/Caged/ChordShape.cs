using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// One string's realized note in a derived <see cref="ChordShape"/>: the chord tone at <see cref="Fret"/>, or a
/// <see cref="Muted"/> string. <see cref="Semitones"/> is the tone's interval from the chord root (0 = root).
/// </summary>
public readonly record struct ChordShapeString(int String, int? Fret, int Semitones)
{
    /// <summary>A muted string (not sounded).</summary>
    public static ChordShapeString Muted(int stringNumber) => new(stringNumber, null, -1);

    /// <summary>Whether this string is muted.</summary>
    public bool IsMuted => Fret is null;
}

/// <summary>
/// A <b>derived</b> CAGED chord shape — the engine output of <c>CagedDerivation.Derive</c>. Carries one
/// <see cref="ChordShapeString"/> per guitar string (low-E→high-E, string 6→1), the derived
/// <see cref="AnchorFinger"/> (req <c>IN2</c>/<c>IN7</c>), and the realized <see cref="OctaveZone"/>. Nothing here
/// is authored: every fret falls out of the quality formula × the octave shape × the interval lattice.
/// </summary>
/// <param name="Quality">The chord quality realized.</param>
/// <param name="Shape">The CAGED octave shape.</param>
/// <param name="Strings">One entry per guitar string, ordered low-E→high-E (string 6 → 1).</param>
/// <param name="AnchorFinger">The finger derived to anchor the shape's root (root's rank in the placed span).</param>
/// <param name="Zone">The octave zone the shape was placed in.</param>
public sealed record ChordShape(
    Quality Quality,
    CagedShape Shape,
    IReadOnlyList<ChordShapeString> Strings,
    Finger AnchorFinger,
    OctaveZone Zone)
{
    /// <summary>The frets low-E→high-E as the voicing DSL prints them: a number, or <c>x</c> for a muted string.</summary>
    public string FretString() =>
        string.Join(' ', Strings.OrderByDescending(s => s.String).Select(s => s.IsMuted ? "x" : s.Fret!.Value.ToString()));
}

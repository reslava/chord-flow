namespace ChordFlow.Domain;

/// <summary>
/// A time signature (v1: 4/4 only — see ctx EX2). Bar and beat lengths in ticks derive from it via
/// the fixed <see cref="TickGrid.Ppq"/> (ctx IN9).
/// </summary>
public readonly record struct TimeSignature(int Numerator, int Denominator)
{
    /// <summary>Common time, 4/4 — the only meter built for v1.</summary>
    public static readonly TimeSignature FourFour = new(4, 4);

    /// <summary>Ticks in one beat (one denominator unit), e.g. a quarter in 4/4 = 48.</summary>
    public int BeatTicks => TickGrid.WholeNoteTicks / Denominator;

    /// <summary>Ticks in a full bar, e.g. 4/4 = 192.</summary>
    public int BarTicks => Numerator * BeatTicks;
}

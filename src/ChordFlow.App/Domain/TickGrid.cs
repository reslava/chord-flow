namespace ChordFlow.Domain;

/// <summary>
/// The fixed tick base for the rhythm model (ctx constraint C1). One quarter note = <see cref="Ppq"/>
/// ticks. 48 is divisible by 4 (sixteenth = 12 ticks) and by 3 (eighth-triplet = 16 ticks), so all
/// common subdivisions coexist in a single grid and patterns compose. There is deliberately no
/// per-pattern grid resolution.
/// </summary>
public static class TickGrid
{
    /// <summary>Pulses (ticks) per quarter note. Fixed at 48.</summary>
    public const int Ppq = 48;

    /// <summary>Ticks in a whole note (4 quarters) = 192.</summary>
    public const int WholeNoteTicks = Ppq * 4;
}

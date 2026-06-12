namespace ChordFlow.Domain;

/// <summary>
/// An arrangement-layer key shift: move the tonic by <see cref="Semitones"/> and, when
/// <see cref="ModeChange"/> is set, switch mode. Applied by the <see cref="SongExpander"/>'s running-key
/// fold — the <see cref="Progression"/> is never mutated (decision A, constraint C2). Absolute resets are
/// modelled separately as <see cref="AbsoluteKey"/>, not here, so this type only ever carries a relative shift.
/// </summary>
public readonly record struct Modulation(int Semitones, bool? ModeChange)
{
    /// <summary>
    /// The key reached by shifting <paramref name="current"/>'s tonic by <see cref="Semitones"/> (folded
    /// mod 12) and switching to <see cref="ModeChange"/> when it is set (otherwise the mode is unchanged).
    /// </summary>
    public Key Apply(Key current)
    {
        ArgumentNullException.ThrowIfNull(current);

        int tonic = Mod12(current.Tonic.Value + Semitones);
        bool isMinor = ModeChange ?? current.IsMinor;
        return new Key(new PitchClass(tonic), isMinor);
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}

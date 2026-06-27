using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The <see cref="VoicingFamily.DoubledShell"/> reduction (shell-voicing-derivation, req IN1): a derived
/// <see cref="ChordShape"/> with its <b>fifth muted</b>, keeping every other sounded string — root, 3rd,
/// 7th/6th and their doublings. A "chord minus the 5th" comping voicing. Pure; muted strings stay muted and
/// nothing is re-packed (req IN4). It inherits the CAGED golden oracle's trust — the surviving notes are
/// already oracle-verified — so it needs no oracle of its own.
/// </summary>
public static class ShellReduction
{
    /// <summary>The <paramref name="shape"/> with every string whose chord-tone function is the fifth muted.</summary>
    public static ChordShape MuteFifth(ChordShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        IReadOnlySet<int> fifths = FifthSemitonesOf(shape.Quality);

        var strings = shape.Strings
            .Select(s => !s.IsMuted && fifths.Contains(Mod12(s.Semitones))
                ? ChordShapeString.Muted(s.String)
                : s)
            .ToList();

        return shape with { Strings = strings };
    }

    // The semitone(s) functioning as the chord's fifth, read from the quality's formula degree (via ChordTones),
    // not a hard-coded 7 — so the b5 of m7b5/dim7 and the #5 of augmented are handled by spelling.
    private static IReadOnlySet<int> FifthSemitonesOf(Quality quality) =>
        ChordTones.Of(new Chord(new PitchClass(0), quality))
            .Where(t => t.Function == ChordToneFunction.Fifth)
            .Select(t => Mod12(t.Interval))
            .ToHashSet();

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}

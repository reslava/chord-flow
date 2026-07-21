namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// How the Pattern strategy draws a bar pattern from a <see cref="RhythmKind"/> for each bar of a phrase
/// (design §3a). <b>Fixed</b> repeats one chosen pattern; <b>Cycle</b> tours the kind (bar N = pattern N —
/// this is also what plays a multi-bar clave in order); <b>RandomInKind</b> draws a seeded random pattern per
/// bar; <b>FixedPlusRotating</b> alternates one fixed pattern with a cycling one (Rafa's fixed+rotating
/// operator, at bar level).
/// </summary>
public abstract record PatternSelection
{
    /// <summary>The bar pattern for <paramref name="barIndex"/> drawn from <paramref name="kind"/>.</summary>
    public abstract OnsetBar BarAt(int barIndex, RhythmKind kind, Random rng);

    private protected static int Mod(int a, int n) => ((a % n) + n) % n;

    /// <summary>One chosen pattern, every bar.</summary>
    public sealed record Fixed(int Index) : PatternSelection
    {
        public override OnsetBar BarAt(int barIndex, RhythmKind kind, Random rng) =>
            kind.Patterns[Mod(Index, kind.Patterns.Count)];
    }

    /// <summary>Tour the kind from <see cref="StartIndex"/> — bar N = pattern (StartIndex + N) wrapping. Also the clave player.</summary>
    public sealed record Cycle(int StartIndex = 0) : PatternSelection
    {
        public override OnsetBar BarAt(int barIndex, RhythmKind kind, Random rng) =>
            kind.Patterns[Mod(StartIndex + barIndex, kind.Patterns.Count)];
    }

    /// <summary>Each bar a seeded random pattern from the kind.</summary>
    public sealed record RandomInKind : PatternSelection
    {
        public override OnsetBar BarAt(int barIndex, RhythmKind kind, Random rng) =>
            kind.Patterns[rng.Next(kind.Patterns.Count)];
    }

    /// <summary>Even bars = the fixed pattern (<see cref="FixedIndex"/>); odd bars = cycle from <see cref="RotatingStartIndex"/>.</summary>
    public sealed record FixedPlusRotating(int FixedIndex, int RotatingStartIndex = 0) : PatternSelection
    {
        public override OnsetBar BarAt(int barIndex, RhythmKind kind, Random rng) =>
            barIndex % 2 == 0
                ? kind.Patterns[Mod(FixedIndex, kind.Patterns.Count)]
                : kind.Patterns[Mod(RotatingStartIndex + barIndex / 2, kind.Patterns.Count)];
    }
}

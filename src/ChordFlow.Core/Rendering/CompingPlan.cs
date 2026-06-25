using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;

namespace ChordFlow.Rendering;

/// <summary>
/// The resolved comping voicings for one render (engine-derived-as-app-source, req IN4/D4=(B)): a
/// <see cref="Chord"/> → <see cref="Voicing"/> map the renderer consumes instead of selecting grips itself.
/// Built in the Features layer by the comping resolver (main-source → fallback → ranking) and handed to
/// <see cref="IScoreRenderer.Render"/>, so the renderer stays a pure formatter and the tab + the now/next
/// chord schedule draw from the same grips (no drift).
/// <para>
/// Keyed by <see cref="Chord"/> value: under the shipped <c>Closest</c> ranking a chord always comps with the
/// same grip (the "reuse this chord's earlier grip" rule), so one entry per distinct chord is exact. A future
/// per-occurrence strategy (variety) would need a richer key — that is the voicing-ranking-strategies thread.
/// </para>
/// </summary>
public sealed class CompingPlan
{
    private readonly IReadOnlyDictionary<Chord, Voicing> _grips;

    public CompingPlan(IReadOnlyDictionary<Chord, Voicing> grips)
    {
        ArgumentNullException.ThrowIfNull(grips);
        _grips = grips;
    }

    /// <summary>The comping grip for <paramref name="chord"/>; throws if the plan never resolved it (fail-loud, C2).</summary>
    public Voicing For(Chord chord) =>
        _grips.TryGetValue(chord, out Voicing? voicing)
            ? voicing
            : throw new InvalidOperationException($"No comping voicing was resolved for {chord.Root.Value}:{chord.Quality}.");
}

using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;

namespace ChordFlow.Rendering;

/// <summary>
/// The resolved comping voicings for one render (engine-derived-as-app-source, req IN4/D4=(B)): a
/// <see cref="Chord"/> → <see cref="Voicing"/> map the renderer consumes instead of selecting grips itself.
/// Built in the Features layer by the comping resolver (main-source → fallback → ranking) and handed to
/// <see cref="IScoreRenderer.Render"/>, so the renderer stays a pure formatter and the tab + the now/next
/// chord schedule draw from the same grips (no drift).
/// <para>
/// Two layers: a <b>per-chord-value</b> map (the ranking fill + the Song's degree/quality <c>voice</c> defaults —
/// under the shipped <c>Closest</c> ranking a chord comps with one grip, so one entry per distinct chord is
/// exact) plus a <b>per-occurrence</b> override map keyed by <see cref="RealizedSpan"/>, holding the per-chord
/// <c>{…}</c> annotations (explicit-voicing-reference IN1/IN5) — an annotation on one occurrence never leaks to
/// the others. <see cref="For(RealizedSpan)"/> checks the override first, then the chord value.
/// </para>
/// </summary>
public sealed class CompingPlan
{
    private static readonly IReadOnlyDictionary<RealizedSpan, Voicing> NoOverrides = new Dictionary<RealizedSpan, Voicing>();

    private readonly IReadOnlyDictionary<Chord, Voicing> _grips;
    private readonly IReadOnlyDictionary<RealizedSpan, Voicing> _spanOverrides;

    public CompingPlan(IReadOnlyDictionary<Chord, Voicing> grips)
        : this(grips, NoOverrides)
    {
    }

    public CompingPlan(IReadOnlyDictionary<Chord, Voicing> grips, IReadOnlyDictionary<RealizedSpan, Voicing> spanOverrides)
    {
        ArgumentNullException.ThrowIfNull(grips);
        ArgumentNullException.ThrowIfNull(spanOverrides);
        _grips = grips;
        _spanOverrides = spanOverrides;
    }

    /// <summary>The comping grip for <paramref name="chord"/>; throws if the plan never resolved it (fail-loud, C2).</summary>
    public Voicing For(Chord chord) =>
        _grips.TryGetValue(chord, out Voicing? voicing)
            ? voicing
            : throw new InvalidOperationException($"No comping voicing was resolved for {chord.Root.Value}:{chord.Quality}.");

    /// <summary>
    /// The comping grip for <paramref name="span"/> — its per-occurrence <c>{…}</c> override if it has one,
    /// else the grip for its chord value (the degree/quality default or the ranking fill).
    /// </summary>
    public Voicing For(RealizedSpan span) =>
        _spanOverrides.TryGetValue(span, out Voicing? overridden) ? overridden : For(span.Chord);
}

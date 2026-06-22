namespace ChordFlow.Music.Progressions.Transforms;

/// <summary>
/// A pure, functional rewrite of a <see cref="Progression"/> — the harmonic analog of how the rhythm
/// overlays (<c>FeelTransform</c> / <c>AccentPattern</c> / <c>StrokeOverlay</c>) operate on timing: never
/// mutate the input, always return a new value. Transforms in this slice operate on key-independent
/// <see cref="Harmony.RomanDegree"/>s, so they need no key. They <b>compose left-to-right</b> and are
/// <b>not commutative</b> — application order is part of the Song-DSL contract.
/// </summary>
public interface IProgressionTransform
{
    /// <summary>Return a new progression derived from <paramref name="progression"/>; never mutate the input.</summary>
    Progression Apply(Progression progression);
}

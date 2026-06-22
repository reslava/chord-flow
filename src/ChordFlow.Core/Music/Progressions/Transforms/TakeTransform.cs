using ChordFlow.Music.Rhythm;

namespace ChordFlow.Music.Progressions.Transforms;

/// <summary>
/// Keep the first <see cref="Count"/> whole bars of a progression and drop the rest — the drill transform
/// for practicing the head / first section of a longer tune. Bars are retained <b>intact</b>, so every
/// per-bar invariant (spans sum to <see cref="TimeSignature.BarTicks"/>, quarter-alignment) holds untouched
/// and the result is rebuilt through the guarded <see cref="Progression.FromBars"/> (4/4-only v1).
/// <para>
/// <see cref="Count"/> must be in <c>[1, Bars.Count]</c>; out of range throws (fail-loud, no clamping).
/// </para>
/// </summary>
public sealed record TakeTransform(int Count) : IProgressionTransform
{
    public Progression Apply(Progression progression)
    {
        ArgumentNullException.ThrowIfNull(progression);

        if (Count < 1 || Count > progression.Bars.Count)
        {
            throw new ArgumentException(
                $"take({Count}) is out of range for a {progression.Bars.Count}-bar progression.", nameof(Count));
        }

        return Progression.FromBars(
            progression.Id,
            progression.Name,
            progression.Bars.Take(Count).ToArray(),
            TimeSignature.FourFour);
    }
}

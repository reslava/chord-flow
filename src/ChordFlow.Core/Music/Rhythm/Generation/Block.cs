namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// One beat's onset pattern — the canonical unit of the generation model (a block <b>is</b> one beat,
/// design §1). <see cref="Subdivision"/> is cells-per-beat (the Rhythm DSL <c>:n</c>: 1 quarter, 2 eighths,
/// 3 triplets, 4 sixteenths); it must divide <see cref="TickGrid.Ppq"/> (the 4/4 beat) so every cell lands
/// on a whole tick. <see cref="Onsets"/> are the cell indices in <c>[0, Subdivision)</c> that carry an
/// attack — an on-beat eighth is <c>Of(2, 0)</c>, the &amp; is <c>Of(2, 1)</c>, both is <c>Of(2, 0, 1)</c>,
/// an empty beat is <c>Empty(n)</c>. Instrument-agnostic and <b>duration-free</b>: whether an onset rings or
/// is an instantaneous hit is decided by a projection, never here.
/// </summary>
public sealed record Block
{
    private Block(int subdivision, IReadOnlyList<int> onsets)
    {
        Subdivision = subdivision;
        Onsets = onsets;
    }

    /// <summary>Cells per beat (the Rhythm DSL <c>:n</c>). Divides <see cref="TickGrid.Ppq"/>.</summary>
    public int Subdivision { get; }

    /// <summary>The attacking cell indices, ascending and distinct, each in <c>[0, Subdivision)</c>.</summary>
    public IReadOnlyList<int> Onsets { get; }

    /// <summary>True when the beat carries no attack (a silent beat).</summary>
    public bool IsEmpty => Onsets.Count == 0;

    /// <summary>
    /// A block at <paramref name="subdivision"/> attacking the given <paramref name="onsets"/> cells.
    /// Cells are sorted and de-duplicated; an out-of-range or duplicate cell throws (fail loud).
    /// </summary>
    public static Block Of(int subdivision, params int[] onsets)
    {
        RequireSubdivision(subdivision);
        var sorted = new SortedSet<int>();
        foreach (int k in onsets)
        {
            if (k < 0 || k >= subdivision)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(onsets), $"Onset cell {k} is outside [0, {subdivision}).");
            }

            if (!sorted.Add(k))
            {
                throw new ArgumentException($"Duplicate onset cell {k}.", nameof(onsets));
            }
        }

        return new Block(subdivision, sorted.ToArray());
    }

    /// <summary>An empty (silent) beat at <paramref name="subdivision"/>.</summary>
    public static Block Empty(int subdivision) => new(RequireSubdivision(subdivision), Array.Empty<int>());

    /// <summary>
    /// The bar-relative onset ticks <b>within this beat</b> (0 = the beat's downbeat): cell <c>k</c> →
    /// <c>k * (beatTicks / Subdivision)</c>. Ascending, matching <see cref="Onsets"/>. The caller adds the
    /// beat offset to place them in the bar (see <see cref="OnsetBar.OnsetTicks"/>).
    /// </summary>
    public IEnumerable<int> OnsetTicks(int beatTicks)
    {
        int cellTicks = beatTicks / Subdivision;
        foreach (int k in Onsets)
        {
            yield return k * cellTicks;
        }
    }

    private static int RequireSubdivision(int subdivision)
    {
        if (subdivision < 1 || TickGrid.Ppq % subdivision != 0)
        {
            throw new ArgumentException(
                $"Subdivision {subdivision} must be ≥ 1 and divide {TickGrid.Ppq}.", nameof(subdivision));
        }

        return subdivision;
    }
}

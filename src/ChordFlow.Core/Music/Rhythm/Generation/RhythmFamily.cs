namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// A named, ordered palette of non-empty <see cref="Block"/>s at one subdivision — the set a Cycle/Rotate/
/// Random draws from (design §3a). The first block is the <see cref="Primary"/> (the strong, on-beat
/// reference an operator places when a beat should sound). v1 ships two families:
/// <list type="bullet">
/// <item><b>Quarter</b> — subdivision 1, blocks <c>{[0]}</c>. The Axis-A family (which beats sound);
///   variation is which beats an operator masks, not intra-beat placement.</item>
/// <item><b>Eighth</b> — subdivision 2, blocks <c>{[0], [1], [0,1]}</c> = on-beat / the &amp; / both. The
///   Axis-B family (placement within the beat).</item>
/// </list>
/// Triplet and 16th families are a later phase (req EX3).
/// </summary>
public sealed record RhythmFamily(string Name, int Subdivision, IReadOnlyList<Block> Blocks)
{
    /// <summary>Quarter family — one block: the beat sounds on its downbeat. Axis A.</summary>
    public static readonly RhythmFamily Quarter =
        new("Quarter", 1, new[] { Block.Of(1, 0) });

    /// <summary>Eighth family — on-beat, the &amp;, and both. Axis B.</summary>
    public static readonly RhythmFamily Eighth =
        new("Eighth", 2, new[] { Block.Of(2, 0), Block.Of(2, 1), Block.Of(2, 0, 1) });

    /// <summary>The v1 families, in offered order.</summary>
    public static readonly IReadOnlyList<RhythmFamily> All = new[] { Quarter, Eighth };

    /// <summary>The strong reference block — the first, on-beat entry. What operators place on a sounding beat.</summary>
    public Block Primary => Blocks[0];

    /// <summary>An empty beat at this family's subdivision — a silent beat within a family bar.</summary>
    public Block Silence => Block.Empty(Subdivision);
}

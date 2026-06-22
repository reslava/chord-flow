using ChordFlow.Music.Progressions;
using ChordFlow.Music.Progressions.Transforms;
using ChordFlow.Music.Harmony;
namespace ChordFlow.Music.Songs;

/// <summary>
/// A named arrangement part — either a reference to a stored <see cref="Progression"/> or one defined inline
/// in the Song's own DSL. The arrangement stream references parts <b>by name</b> (and names recur, e.g.
/// <c>A x2 … A</c>), so parts live in a dictionary on the <see cref="Song"/> and the stream stays a flat list
/// of cheap references.
/// </summary>
public abstract record Part(string Name);

/// <summary>
/// A reference to a stored progression, resolved against the <see cref="IProgressionStore"/> at expand time
/// (the factory only checks the id is non-empty; the lookup belongs to <see cref="SongExpander"/>, which has
/// the store — keeping <c>Domain/</c> I/O-free, constraint C3).
/// </summary>
public sealed record ProgressionReference(string Name, string ProgressionId) : Part(Name);

/// <summary>An inline progression defined locally in the Song's own DSL (self-contained, immune to store deletes).</summary>
public sealed record InlineProgression(string Name, Progression Progression) : Part(Name);

/// <summary>One entry in a Song's ordered arrangement stream.</summary>
public abstract record ArrangementItem;

/// <summary>
/// Play a named part <see cref="Repeat"/> times (<c>Repeat &gt;= 1</c>), optionally rewriting the resolved
/// progression with an ordered list of <see cref="Transforms"/> applied left-to-right at realization. The
/// transform list lives on the <i>play</i> (the application site), not the <see cref="Part"/> definition, so
/// the same part can be played plain in one spot and transformed in another. Expansion happens at
/// realization, never in the parser, so the Song stays a compact stream (constraint C5).
/// </summary>
public sealed record PartPlay(
    string PartName,
    int Repeat,
    IReadOnlyList<IProgressionTransform> Transforms) : ArrangementItem
{
    /// <summary>Convenience for the no-transform case — keeps transform-unaware call sites unchanged.</summary>
    public PartPlay(string partName, int repeat)
        : this(partName, repeat, Array.Empty<IProgressionTransform>())
    {
    }
}

/// <summary>A relative modulation between parts — accumulates over the running key (constraint C2).</summary>
public sealed record RelativeMod(Modulation Modulation) : ArrangementItem;

/// <summary>An absolute key reset between parts — the escape hatch that returns home (decision C).</summary>
public sealed record AbsoluteKey(Key Key) : ArrangementItem;

/// <summary>
/// A <b>Song</b>: an arrangement of <see cref="Progression"/>s. It composes <b>references</b> plus
/// arrangement instructions (repetition, modulation, section order) only — it never holds bars or chords
/// directly, so harmony stays in the Progression (constraint C1). A Song cannot be played on its own; the
/// play unit is <see cref="Exercise"/> (Song + Comping + optional Lead + params). A bare
/// <see cref="Progression"/> is lifted into a single-section Song via <see cref="OfProgression"/> so there
/// is one realization path (no Progression-vs-Song branch downstream).
/// <para>
/// All construction funnels through the guarded factory <see cref="FromSections"/> (paralleling
/// <see cref="Progression.FromBars"/>), so a malformed Song is unconstructable.
/// </para>
/// </summary>
public sealed record Song
{
    public string Id { get; }

    public string Name { get; }

    /// <summary>The key the realization fold starts from; modulations accumulate from here (decision C/E).</summary>
    public Key InitialKey { get; }

    /// <summary>Local part definitions, keyed by name (inline progressions and stored references alike).</summary>
    public IReadOnlyDictionary<string, Part> Parts { get; }

    /// <summary>The ordered arrangement stream: plays, relative modulations, and absolute key resets.</summary>
    public IReadOnlyList<ArrangementItem> Items { get; }

    // Private full constructor: the only way fields reach the record. The public entry point validates first.
    private Song(
        string id,
        string name,
        Key initialKey,
        IReadOnlyDictionary<string, Part> parts,
        IReadOnlyList<ArrangementItem> items)
    {
        Id = id;
        Name = name;
        InitialKey = initialKey;
        Parts = parts;
        Items = items;
    }

    /// <summary>
    /// Guarded factory — the only constructor. Validates:
    /// <list type="bullet">
    /// <item>every <see cref="PartPlay.PartName"/> resolves to a <see cref="Part"/> in <paramref name="parts"/>;</item>
    /// <item>every <see cref="ProgressionReference.ProgressionId"/> is non-empty (store resolution is deferred to <see cref="SongExpander"/>);</item>
    /// <item>every <see cref="PartPlay.Repeat"/> is <c>&gt;= 1</c>;</item>
    /// <item>at least one <see cref="PartPlay"/> exists (a Song with no parts to play is meaningless).</item>
    /// </list>
    /// Throws <see cref="ArgumentException"/> naming the offending item — the same convention as
    /// <see cref="Progression.FromBars"/> (the parser layer raises <see cref="FormatException"/> for grammar errors).
    /// </summary>
    public static Song FromSections(
        string id,
        string name,
        Key initialKey,
        IReadOnlyDictionary<string, Part> parts,
        IReadOnlyList<ArrangementItem> items)
    {
        ArgumentNullException.ThrowIfNull(initialKey);
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(items);

        foreach ((string partName, Part part) in parts)
        {
            if (part is ProgressionReference { ProgressionId.Length: 0 })
            {
                throw new ArgumentException(
                    $"Part \"{partName}\" is a stored reference with an empty progression id.", nameof(parts));
            }
        }

        int playCount = 0;
        foreach (ArrangementItem item in items)
        {
            if (item is not PartPlay play)
            {
                continue;
            }

            playCount++;

            if (play.Repeat < 1)
            {
                throw new ArgumentException(
                    $"Part play \"{play.PartName}\" has repeat {play.Repeat}; must be >= 1.", nameof(items));
            }

            if (!parts.ContainsKey(play.PartName))
            {
                throw new ArgumentException(
                    $"Part play references unknown part \"{play.PartName}\".", nameof(items));
            }
        }

        if (playCount == 0)
        {
            throw new ArgumentException("A Song must play at least one part.", nameof(items));
        }

        return new Song(id, name, initialKey, parts, items);
    }

    /// <summary>
    /// Lift a bare <paramref name="progression"/> into a single-section Song (one inline part "A" played
    /// once) anchored at <paramref name="initialKey"/>. This is the trivial bridge that keeps a simple
    /// one-progression drill on the same <see cref="SongExpander.Expand"/> → render path as a full
    /// arrangement — no <c>Progression</c>-vs-<c>Song</c> branching downstream (IN2). The Song reuses the
    /// progression's id/name so it stays traceable to its source.
    /// </summary>
    public static Song OfProgression(Progression progression, Key initialKey)
    {
        ArgumentNullException.ThrowIfNull(progression);

        return FromSections(
            progression.Id,
            progression.Name,
            initialKey,
            new Dictionary<string, Part> { ["A"] = new InlineProgression("A", progression) },
            new ArrangementItem[] { new PartPlay("A", 1) });
    }
}

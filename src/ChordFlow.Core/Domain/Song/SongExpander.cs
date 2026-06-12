namespace ChordFlow.Domain;

/// <summary>
/// Realizes a <see cref="Song"/> into a <see cref="RealizedSong"/>: resolves each part (local-first, then the
/// <see cref="IProgressionStore"/>), folds modulations left-to-right over a running key, and expands repeats.
/// Slots in <b>above</b> <see cref="Transposer"/> — nothing in <c>Domain/</c> harmony, <c>Rendering/</c>, or
/// the bridge below it changes (design principle). Pure apart from the store lookup, the one I/O seam (C3).
/// </summary>
public static class SongExpander
{
    /// <summary>
    /// Expand <paramref name="song"/> against <paramref name="store"/>. The fold carries a running key:
    /// <see cref="AbsoluteKey"/> resets it, <see cref="RelativeMod"/> accumulates onto it
    /// (<see cref="Modulation.Apply"/>), and <see cref="PartPlay"/> appends <c>Repeat</c> copies of the
    /// resolved progression realized in the current key. <see cref="RealizedSection.Key"/> is therefore an
    /// output of the fold, never an input (decision E).
    /// </summary>
    public static RealizedSong Expand(Song song, IProgressionStore store)
    {
        ArgumentNullException.ThrowIfNull(song);
        ArgumentNullException.ThrowIfNull(store);

        Key key = song.InitialKey;
        var sections = new List<RealizedSection>();

        foreach (ArrangementItem item in song.Items)
        {
            switch (item)
            {
                case AbsoluteKey absolute:
                    key = absolute.Key;
                    break;

                case RelativeMod relative:
                    key = relative.Modulation.Apply(key);
                    break;

                case PartPlay play:
                    Progression progression = Resolve(play.PartName, song, store);
                    IReadOnlyList<RealizedBar> bars = Transposer.RealizeBars(progression, key);
                    for (int i = 0; i < play.Repeat; i++)
                    {
                        sections.Add(new RealizedSection(play.PartName, key, bars));
                    }

                    break;

                default:
                    throw new ArgumentException(
                        $"Unknown arrangement item type {item.GetType().Name}.", nameof(song));
            }
        }

        return new RealizedSong(sections);
    }

    // Resolve a part name to a concrete progression: inline parts are self-contained; stored references hit
    // the store and fail loud if the row is gone (constraint C4 — never silently drop a section). The Song's
    // Parts dict was populated local-first by the parser, so an inline definition already shadows a stored one.
    private static Progression Resolve(string name, Song song, IProgressionStore store)
    {
        if (!song.Parts.TryGetValue(name, out Part? part))
        {
            // FromSections guarantees membership; this guards programmatic Songs that bypassed the factory.
            throw new InvalidOperationException($"Part \"{name}\" is not defined.");
        }

        return part switch
        {
            InlineProgression inline => inline.Progression,
            ProgressionReference reference =>
                store.Find(reference.ProgressionId)
                    ?? throw new InvalidOperationException($"reference '{reference.Name}' not found"),
            _ => throw new InvalidOperationException($"Unknown part type {part.GetType().Name}."),
        };
    }
}

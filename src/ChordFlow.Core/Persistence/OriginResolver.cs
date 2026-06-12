namespace ChordFlow.Persistence;

/// <summary>
/// Shadow-resolves content definitions across provenance tiers: for each <see cref="IOriginated.Id"/>, the
/// highest-precedence copy wins — <c>UserDefined &gt; Pack &gt; BuiltIn</c> (design §6.1). Pure and
/// read-only: it <i>selects</i>, never mutates, so lower tiers remain available as fallback — removing a
/// local copy lets the next tier down win on the next resolve ("non-destructive shadowing"). One Id-keyed
/// resolver shared by every content entity (the same shadowing law the song and voicings threads adopt).
/// The storage model that lets tiers physically coexist is the import layer's concern; this is the
/// selection policy over whatever candidate set it is given.
/// </summary>
public static class OriginResolver
{
    /// <summary>Precedence rank — higher wins: <c>UserDefined</c> (2) &gt; <c>Pack</c> (1) &gt; <c>BuiltIn</c> (0).</summary>
    public static int Rank(Origin origin) => origin switch
    {
        Origin.UserDefined => 2,
        Origin.Pack => 1,
        Origin.BuiltIn => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unhandled origin."),
    };

    /// <summary>
    /// The effective set: one winner per id — the highest-ranked copy. Winners appear in first-seen id
    /// order; a rank tie (two copies of the same id at the same origin) keeps the first-seen candidate.
    /// </summary>
    public static IReadOnlyList<T> Resolve<T>(IEnumerable<T> candidates) where T : IOriginated
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var winners = new Dictionary<string, T>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (T candidate in candidates)
        {
            if (winners.TryGetValue(candidate.Id, out T? current))
            {
                if (Rank(candidate.Origin) > Rank(current.Origin))
                {
                    winners[candidate.Id] = candidate;
                }
            }
            else
            {
                winners[candidate.Id] = candidate;
                order.Add(candidate.Id);
            }
        }

        return order.Select(id => winners[id]).ToList();
    }

    /// <summary>
    /// The winning copy for a single <paramref name="id"/> among <paramref name="candidates"/>, or
    /// <c>null</c> when none share that id. A rank tie keeps the first-seen candidate.
    /// </summary>
    public static T? ResolveOne<T>(IEnumerable<T> candidates, string id) where T : class, IOriginated
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(id);

        T? best = null;
        foreach (T candidate in candidates)
        {
            if (string.Equals(candidate.Id, id, StringComparison.Ordinal) &&
                (best is null || Rank(candidate.Origin) > Rank(best.Origin)))
            {
                best = candidate;
            }
        }

        return best;
    }
}

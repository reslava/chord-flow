namespace ChordFlow.Persistence;

/// <summary>
/// Resolves a single content definition across provenance tiers when more than one row shares an id: the
/// highest-precedence copy wins — <c>UserDefined &gt; Pack</c>. Pure and read-only. Under the multi-source
/// model (content-source-model) the <i>list</i> path no longer collapses — every source is shown — so this
/// resolver is used only by the <b>single-item</b> read paths (<see cref="ResolveOne"/>: <c>Get</c>/<c>Find</c>)
/// and by <see cref="Resolve"/> (the voicing book's load). With fork-on-edit minting unique ids, an id
/// usually has exactly one row; the ranking is the defensive tiebreak for any legacy duplicate.
/// </summary>
public static class OriginResolver
{
    /// <summary>Precedence rank — higher wins: <c>UserDefined</c> (1) &gt; <c>Pack</c> (0).</summary>
    public static int Rank(Origin origin) => origin switch
    {
        Origin.UserDefined => 1,
        Origin.Pack => 0,
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

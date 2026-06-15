namespace ChordFlow.Persistence;

/// <summary>
/// The uniform write/read surface the content-CRUD feature drives, implemented by every content store
/// (progression, song, rhythm, voicing). It deliberately speaks in <b>DSL strings</b>, not parsed domain
/// types, because that is exactly what the editor authors and what the store persists — the parse is an
/// internal validation step. All four entities share this one shape (design §3: one generic surface, not
/// four divergent ones); the feature layer maps an entity discriminator to the right store.
///
/// <para><b>Tier law (C2).</b> CRUD only ever writes the <see cref="Origin.UserDefined"/> tier. Editing a
/// BuiltIn/Pack definition writes a <c>(id, UserDefined)</c> <i>shadow</i> — the lower row is never touched
/// (<see cref="OriginResolver"/> makes the shadow win on the next read). Deleting removes only the
/// UserDefined row: it is gone if it was user-only (<see cref="DeleteOutcome.Deleted"/>) or it reverts to the
/// shadowed lower tier if one exists (<see cref="DeleteOutcome.Reverted"/>).</para>
/// </summary>
public interface IContentStore
{
    /// <summary>One <see cref="ContentSummary"/> per id (the resolved winning tier), for the list pane.</summary>
    IReadOnlyList<ContentSummary> List();

    /// <summary>The editable form of one definition (resolved winning tier), or null if no such id exists.</summary>
    ContentDoc? Get(string id);

    /// <summary>
    /// Create or update a definition in the <see cref="Origin.UserDefined"/> tier and return its id. A
    /// null/blank <paramref name="id"/> creates a new definition (fresh GUID); a non-blank id upserts the
    /// <c>(id, UserDefined)</c> row (a new shadow when none existed). Validates by parsing <paramref name="dsl"/>
    /// first — a malformed definition throws <see cref="System.FormatException"/> and writes nothing.
    /// </summary>
    string Save(string? id, string name, string dsl);

    /// <summary>Remove the UserDefined row for <paramref name="id"/>; the outcome says whether it vanished or reverted.</summary>
    DeleteOutcome Delete(string id);
}

/// <summary>A list-row view of one definition: its id, display name, the resolved winning <see cref="Origin"/>,
/// and whether a lower tier exists under it (so the UI labels the destructive action "Delete" vs "Revert").</summary>
public sealed record ContentSummary(string Id, string Name, Origin Origin, bool HasLowerTier);

/// <summary>The editable payload of one definition: id, display name, and the header-stripped DSL body.</summary>
public sealed record ContentDoc(string Id, string Name, string Dsl);

/// <summary>What a <see cref="IContentStore.Delete"/> did.</summary>
public enum DeleteOutcome
{
    /// <summary>No UserDefined row for that id existed — nothing was removed.</summary>
    NotFound,

    /// <summary>The UserDefined row was the only tier; the definition is gone.</summary>
    Deleted,

    /// <summary>The UserDefined row was a shadow; removing it reverts to the lower (Pack/BuiltIn) tier.</summary>
    Reverted,
}

/// <summary>
/// Pure helper shared by every <see cref="IContentStore.List"/>: collapse the tiered rows to one
/// <see cref="ContentSummary"/> per id (the winning tier via <see cref="OriginResolver"/>), flagging whether
/// a lower tier exists under the winner. Kept here so the four stores don't each re-derive the resolution
/// math; the EF row access stays in each (concrete) store.
/// </summary>
internal static class ContentSummaries
{
    public static IReadOnlyList<ContentSummary> Build(IEnumerable<(string Id, string Name, Origin Origin)> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var summaries = new List<ContentSummary>();
        foreach (var group in rows.GroupBy(r => r.Id, StringComparer.Ordinal))
        {
            (string Id, string Name, Origin Origin) winner = group
                .Aggregate((best, next) =>
                    OriginResolver.Rank(next.Origin) > OriginResolver.Rank(best.Origin) ? next : best);
            bool hasLowerTier = group.Any(r => OriginResolver.Rank(r.Origin) < OriginResolver.Rank(winner.Origin));
            summaries.Add(new ContentSummary(winner.Id, winner.Name, winner.Origin, hasLowerTier));
        }

        return summaries.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

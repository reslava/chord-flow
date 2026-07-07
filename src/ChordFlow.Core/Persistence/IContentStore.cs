namespace ChordFlow.Persistence;

/// <summary>
/// The uniform write/read surface the content-CRUD feature drives, implemented by every content store
/// (progression, song, rhythm, voicing). It deliberately speaks in <b>DSL strings</b>, not parsed domain
/// types, because that is exactly what the editor authors and what the store persists — the parse is an
/// internal validation step. All four entities share this one shape (design §3: one generic surface, not
/// four divergent ones); the feature layer maps an entity discriminator to the right store.
///
/// <para><b>Multi-source model (content-source-model).</b> Sources never hide each other: <see cref="List"/>
/// returns one <see cref="ContentSummary"/> per <i>(id, source)</i> — no tier collapse — each tagged with its
/// <see cref="ContentSource"/> (a pack, by id, or the user). Writes are <b>user-only and fork-on-edit</b>:
/// <see cref="Save"/> updates an existing <see cref="Origin.UserDefined"/> row in place, but editing a
/// package item (or any non-user id) forks a <b>new</b> user row with a fresh id — never a same-id shadow, so
/// the package original stays listed. <see cref="Delete"/> removes only the user row (<see cref="DeleteOutcome.Deleted"/>);
/// there is no "revert" because nothing was ever overridden.</para>
/// </summary>
public interface IContentStore
{
    /// <summary>One <see cref="ContentSummary"/> per (id, source), for the list pane — every source shown, none collapsed.</summary>
    IReadOnlyList<ContentSummary> List();

    /// <summary>The editable form of one definition (resolved to a single row by id), or null if no such id exists.</summary>
    ContentDoc? Get(string id);

    /// <summary>
    /// Create or update a definition in the <see cref="Origin.UserDefined"/> tier and return its id. Update
    /// happens in place only when a <c>(id, UserDefined)</c> row already exists; otherwise — a null/blank id,
    /// or an id that belongs to a package — a <b>new</b> user row with a fresh GUID is created (fork-on-edit,
    /// never a same-id shadow). Validates by parsing <paramref name="dsl"/> first — a malformed definition
    /// throws <see cref="System.FormatException"/> and writes nothing.
    /// </summary>
    string Save(string? id, string name, string dsl);

    /// <summary>Remove the UserDefined row for <paramref name="id"/>; <see cref="DeleteOutcome.Deleted"/> if it existed, else <see cref="DeleteOutcome.NotFound"/>.</summary>
    DeleteOutcome Delete(string id);
}

/// <summary>
/// The source (provenance) of a listed content item — what the UI tags and filters on (content-source-model).
/// </summary>
public enum ContentSource
{
    /// <summary>Engine-derived (computed, never stored). The DB stores never produce this; a computed source unions it in.</summary>
    Automatic,

    /// <summary>Imported from a content pack — <see cref="ContentSummary.PackId"/> names which pack.</summary>
    Package,

    /// <summary>User-authored.</summary>
    User,
}

/// <summary>A list-row view of one definition: its id, display name, its <see cref="Source"/>, and (when
/// <see cref="Source"/> is <see cref="ContentSource.Package"/>) the source pack's <see cref="PackId"/> (else
/// null). <paramref name="InitialKey"/> is the song's starting-key tonic pitch class (0..11) — set only by
/// <see cref="SongStore"/> so the Practice key picker can seed from it (play-ui-key-init IN1); null for the
/// key-independent entities (progression/rhythm/voicing). <paramref name="DefaultFeel"/> is the song's
/// <see cref="Song.DefaultFeel"/> ident ("None"/"Triplet8th"/"Triplet16th") — also set only by
/// <see cref="SongStore"/> so the feel control can seed (song-default-feel IN4); null when the song declares
/// no feel or for the feel-independent entities. <paramref name="DefaultTempo"/> is the song's
/// <see cref="Song.DefaultTempo"/> (BPM) — also set only by <see cref="SongStore"/> so the tempo control can
/// seed (scorer-render-params IN1); null when the song declares no tempo or for the other entities.</summary>
public sealed record ContentSummary(
    string Id, string Name, ContentSource Source, string? PackId, int? InitialKey = null, string? DefaultFeel = null,
    int? DefaultTempo = null);

/// <summary>The editable payload of one definition: id, display name, and the header-stripped DSL body.</summary>
public sealed record ContentDoc(string Id, string Name, string Dsl);

/// <summary>What a <see cref="IContentStore.Delete"/> did.</summary>
public enum DeleteOutcome
{
    /// <summary>No UserDefined row for that id existed — nothing was removed.</summary>
    NotFound,

    /// <summary>The UserDefined row was removed; the definition is gone.</summary>
    Deleted,
}

/// <summary>
/// Pure helper shared by every <see cref="IContentStore.List"/>: project the rows to one
/// <see cref="ContentSummary"/> per row — <b>no collapse</b> (content-source-model: every source is shown) —
/// tagging each with its <see cref="ContentSource"/> and carrying the <c>PackId</c> for package rows. Kept
/// here so the four stores share the projection; the EF row access stays in each (concrete) store.
/// </summary>
internal static class ContentSummaries
{
    /// <summary>Map a stored provenance tier to its list-facing source kind.</summary>
    public static ContentSource SourceOf(Origin origin) => origin switch
    {
        Origin.UserDefined => ContentSource.User,
        Origin.Pack => ContentSource.Package,
        _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unhandled origin."),
    };

    public static IReadOnlyList<ContentSummary> Build(IEnumerable<(string Id, string Name, Origin Origin, string? PackId)> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return rows
            .Select(r => new ContentSummary(
                r.Id, r.Name, SourceOf(r.Origin), r.Origin == Origin.Pack ? r.PackId : null))
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

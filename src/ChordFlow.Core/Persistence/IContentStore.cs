using ChordFlow.Music.Progressions;

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
    /// <para><paramref name="sourceId"/> is the id the editor was showing when the save fired (the fork-from
    /// source). The preserved catalog header is the baseline the write builds on — an in-place edit keeps the
    /// row's own header and a fork inherits the source row's header (genre/subgenre/tags/description/
    /// <c>tonality:</c>), so a minor progression keeps its <c>tonality:</c> across fork/edit instead of silently
    /// realizing as major. Null ⇒ no source ⇒ a brand-new definition with no header.</para>
    /// <para><paramref name="tonality"/> is the <b>explicit</b> author choice from the editor's tonality control:
    /// when non-null it overrides the preserved/source tonality (authoring a new minor progression, or writing a
    /// major↔minor flip); null ⇒ leave the preserved source tonality untouched. Only <see cref="ProgressionStore"/>
    /// acts on it in v1 (a song's mode is its <c>key</c>/<c>mod</c> stream); the other stores accept it inertly.</para>
    /// <para><paramref name="metadata"/> is the editor's <b>authoritative</b> genre/subgenre/tags patch
    /// (content-metadata-editing IN5). When non-null it overlays those three fields onto the preserved header
    /// (keeping description + tonality — C4) and is also written into the denormalized <c>ICatalogEntity</c>
    /// columns (IN6); a present-but-empty field <b>clears</b> it (IN9). Null ⇒ "not edited" ⇒ the preserved
    /// header is kept verbatim (the Rhythm store — no catalog metadata, EX1 — always receives null).</para>
    /// </summary>
    string Save(string? id, string name, string dsl, string? sourceId = null, Tonality? tonality = null, CatalogMetadataPatch? metadata = null);

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
/// <para><paramref name="Genre"/> / <paramref name="Subgenre"/> / <paramref name="Tags"/> are the catalog
/// metadata surfaced for the list fields + the shared FilterR (filter-toggle-buttons IN1). Read from the row's
/// own DSL header (<see cref="CatalogHeader"/>, the canonical source — the denormalized entity columns are now
/// populated on save too, but <see cref="List"/> still reads the header; switching the read path is deferred),
/// so a fork shows its inherited header. Empty for rhythm patterns (no catalog metadata — EX3).</para>
public sealed record ContentSummary(
    string Id, string Name, ContentSource Source, string? PackId, int? InitialKey = null, string? DefaultFeel = null,
    int? DefaultTempo = null, bool? InitialKeyIsMinor = null, // tonality mode: true=minor, false=major, null=n/a (rhythm/voicing)
    string? Genre = null, string? Subgenre = null, IReadOnlyList<string>? Tags = null);

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

    public static IReadOnlyList<ContentSummary> Build(
        IEnumerable<(string Id, string Name, Origin Origin, string? PackId, CatalogMetadata Meta)> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return rows
            .Select(r => new ContentSummary(
                r.Id, r.Name, SourceOf(r.Origin), r.Origin == Origin.Pack ? r.PackId : null,
                Genre: r.Meta.Genre, Subgenre: r.Meta.Subgenre, Tags: r.Meta.Tags))
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

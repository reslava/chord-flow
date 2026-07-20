using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// DB-backed CRUD for stored <see cref="Song"/> definitions — the content store that did not exist before the
/// content-CRUD thread (the <c>Songs</c> table + <see cref="SongEntity"/> + <see cref="SongParser"/> were
/// there, but nothing read or wrote songs by id). Mirrors <see cref="ProgressionStore"/>: the canonical Song
/// DSL is the only persisted form (alphaTex/realized songs are regenerated on load — C4), the stored <c>Dsl</c>
/// may carry a catalog header that the editor never sees (EX3), and writes target the <see cref="Origin.UserDefined"/>
/// tier only (C2). Full realization (resolving stored <c>ProgressionReference</c>s) needs an
/// <see cref="IProgressionStore"/> and lives in <see cref="SongExpander"/>; this store only validates the
/// arrangement grammar on save.
/// </summary>
public sealed class SongStore : IContentStore
{
    private readonly ChordFlowDbContext _db;
    private readonly TimeSignature _ts;

    public SongStore(ChordFlowDbContext db, TimeSignature? ts = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
        _ts = ts ?? TimeSignature.FourFour;
    }

    /// <inheritdoc/>
    /// <remarks>Unlike the other stores, the song summary carries the play-time seeds parsed from the winning
    /// tier's own DSL: <see cref="ContentSummary.InitialKey"/> (the <see cref="Song.InitialKey"/> tonic, so the
    /// Practice key picker can seed — play-ui-key-init IN1) and <see cref="ContentSummary.DefaultFeel"/> (the
    /// <see cref="Song.DefaultFeel"/> ident, so the feel control can seed — song-default-feel IN4).</remarks>
    public IReadOnlyList<ContentSummary> List()
    {
        List<SongEntity> rows = _db.Songs.AsNoTracking().ToList();
        return ContentSummaries.Build(rows.Select(s => (s.Id, s.Name, s.Origin, s.PackId, CatalogHeader.Parse(s.Dsl).Metadata)))
            .Select(summary =>
            {
                (int? key, string? feel, int? tempo, bool? isMinor) = SeedsOf(rows, summary.Id);
                return summary with { InitialKey = key, DefaultFeel = feel, DefaultTempo = tempo, InitialKeyIsMinor = isMinor };
            })
            .ToList();
    }

    // The winning tier's play-time seeds derived from its own DSL: the Song.InitialKey tonic (0..11), the
    // Song.DefaultFeel ident ("None"/"Triplet8th"/"Triplet16th", or null when the song declares no feel), and the
    // Song.DefaultTempo BPM (or null when the song declares no tempo) — all values ExerciseRendering / the
    // transport fall back to, never a second stored copy (C5). A malformed or unresolved song yields all-null so
    // it still lists, just without seeds.
    private (int? Key, string? Feel, int? Tempo, bool? IsMinor) SeedsOf(IReadOnlyList<SongEntity> rows, string id)
    {
        SongEntity? winner = OriginResolver.ResolveOne(rows.Where(r => r.Id == id).ToList(), id);
        if (winner is null)
        {
            return (null, null, null, null);
        }

        try
        {
            (_, string body) = CatalogHeader.Parse(winner.Dsl);
            Song song = SongParser.Parse(winner.Id, winner.Name, body, _ts);
            return (song.InitialKey.Tonic.Value, song.DefaultFeel?.ToString(), song.DefaultTempo, song.InitialKey.IsMinor);
        }
        catch (FormatException)
        {
            return (null, null, null, null);
        }
    }

    /// <inheritdoc/>
    public ContentDoc? Get(string id)
    {
        List<SongEntity> rows = _db.Songs.AsNoTracking().Where(s => s.Id == id).ToList();
        SongEntity? row = OriginResolver.ResolveOne(rows, id);
        if (row is null)
        {
            return null;
        }

        // The editor authors the arrangement grammar; strip any catalog header (metadata editing is EX3).
        (_, string body) = CatalogHeader.Parse(row.Dsl);
        return new ContentDoc(row.Id, row.Name, body);
    }

    /// <inheritdoc/>
    public string Save(string? id, string name, string dsl, string? sourceId = null, Tonality? tonality = null, CatalogMetadataPatch? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dsl);

        // A song's mode is its `key`/`mod` stream, not a `tonality:` header (EX4) — the explicit tonality is inert.
        _ = tonality;
        (_, string body) = CatalogHeader.Parse(dsl);

        // User-only, fork-on-edit (content-source-model): update an existing user row in place; a blank id or
        // a non-user id (e.g. editing a package item) forks a new user row with a fresh id — never a shadow.
        SongEntity? row = string.IsNullOrWhiteSpace(id) ? null : _db.Songs.Find(id, Origin.UserDefined);
        string targetId = row?.Id ?? Guid.NewGuid().ToString();
        Validate(targetId, name, body);

        // The preserved header is the baseline: the in-place row's own, else the forked-from source's. The
        // editor's authoritative genre/subgenre/tags patch (content-metadata-editing IN5) overlays those three
        // fields, keeping description + tonality (C4); no patch ⇒ preserve verbatim.
        CatalogMetadata preserved = row is not null
            ? CatalogHeader.Parse(row.Dsl).Metadata
            : SourceMetadata(sourceId ?? id);
        CatalogMetadata meta = metadata is not null ? metadata.ApplyTo(preserved) : preserved;
        string storedDsl = CatalogHeader.Serialize(meta, body);

        if (row is null)
        {
            _db.Songs.Add(new SongEntity
            {
                Id = targetId,
                Name = name,
                Dsl = storedDsl,
                Genre = meta.Genre,
                Subgenre = meta.Subgenre,
                Tags = CatalogHeader.SerializeTags(meta.Tags),
                Origin = Origin.UserDefined,
                CreatedUtc = DateTime.UtcNow,
            });
        }
        else
        {
            row.Name = name;
            row.Dsl = storedDsl;
            row.Genre = meta.Genre;
            row.Subgenre = meta.Subgenre;
            row.Tags = CatalogHeader.SerializeTags(meta.Tags);
        }

        _db.SaveChanges();
        return targetId;
    }

    // The catalog metadata to preserve when the editor didn't (and can't, EX3) carry it: resolve the source
    // definition (across origins) and read its header. Empty when there is no source, or it carries no header.
    private CatalogMetadata SourceMetadata(string? sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return CatalogMetadata.Empty;
        }

        List<SongEntity> rows = _db.Songs.AsNoTracking().Where(s => s.Id == sourceKey).ToList();
        SongEntity? src = OriginResolver.ResolveOne(rows, sourceKey);
        return src is null ? CatalogMetadata.Empty : CatalogHeader.Parse(src.Dsl).Metadata;
    }

    /// <inheritdoc/>
    public DeleteOutcome Delete(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        SongEntity? row = _db.Songs.Find(id, Origin.UserDefined);
        if (row is null)
        {
            return DeleteOutcome.NotFound;
        }

        _db.Songs.Remove(row);
        _db.SaveChanges();
        return DeleteOutcome.Deleted;
    }

    /// <summary>
    /// Find a stored song by id and parse it into a <see cref="Song"/>, or null if absent. Resolves the
    /// highest tier per id (UserDefined &gt; Pack &gt; BuiltIn) like the other stores, then parses the
    /// arrangement grammar only — stored <see cref="ProgressionReference"/>s are resolved later by
    /// <see cref="SongExpander"/> (which has the <see cref="IProgressionStore"/>), keeping this a pure read.
    /// </summary>
    public Song? Find(string id)
    {
        List<SongEntity> rows = _db.Songs.AsNoTracking().Where(s => s.Id == id).ToList();
        SongEntity? row = OriginResolver.ResolveOne(rows, id);
        if (row is null)
        {
            return null;
        }

        // The DSL may carry a catalog header; the parser only ever sees the arrangement grammar.
        (_, string body) = CatalogHeader.Parse(row.Dsl);
        return SongParser.Parse(row.Id, row.Name, body, _ts);
    }

    // SongParser raises FormatException for grammar errors but Song.FromSections raises ArgumentException for
    // structural ones (no plays, unknown part, …). Normalize the latter to a FormatException so the CRUD
    // parse-error surface (IN3) sees one "invalid definition" exception type for every entity.
    private void Validate(string id, string name, string body)
    {
        try
        {
            SongParser.Parse(id, name, body, _ts);
        }
        catch (ArgumentException ex) when (ex is not ArgumentNullException)
        {
            throw new FormatException(ex.Message, ex);
        }
    }
}

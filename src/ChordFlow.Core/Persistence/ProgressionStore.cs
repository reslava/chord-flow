using ChordFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// The DB-backed implementation of <see cref="IProgressionStore"/> — the concrete seam <see cref="SongExpander"/>
/// uses to resolve a <see cref="ProgressionReference"/> to its stored <see cref="Progression"/>. Lives in
/// <c>Persistence/</c> (it touches the DB); <c>Domain/</c> only ever sees the interface (constraint C3). The
/// stored <c>Dsl</c> is header-stripped and re-parsed on each lookup — the same "store the definition,
/// regenerate on load" pattern the rest of persistence uses (alphaTex/realized forms are never stored).
/// </summary>
public sealed class ProgressionStore : IProgressionStore
{
    private readonly ChordFlowDbContext _db;
    private readonly TimeSignature _ts;

    public ProgressionStore(ChordFlowDbContext db, TimeSignature? ts = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
        _ts = ts ?? TimeSignature.FourFour;
    }

    public Progression? Find(string id)
    {
        Entities.ProgressionEntity? row = _db.Progressions.AsNoTracking().FirstOrDefault(p => p.Id == id);
        if (row is null)
        {
            return null;
        }

        // The DSL may carry a catalog header; the pure ProgressionParser only ever sees the bar grammar.
        (_, string body) = CatalogHeader.Parse(row.Dsl);
        return ProgressionParser.Parse(row.Id, row.Name, body, _ts);
    }
}

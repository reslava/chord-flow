using ChordFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// DB-backed lookup of stored rhythm patterns — reconstructs a <see cref="RhythmPattern"/> by parsing the
/// row's canonical <c>Dsl</c> with its stored time signature. Lives in <c>Persistence/</c> (it touches the
/// DB); <c>Domain/</c> stays I/O-free (constraint C2). The stored <c>Dsl</c> is re-parsed on each lookup —
/// the same "store the definition, regenerate on load" pattern as <see cref="ProgressionStore"/> (the
/// parsed tick grid and alphaTex are never stored — C1). No catalog-header strip: rhythm patterns carry no
/// catalog metadata (EX3). Concrete (no Domain interface yet — nothing in <c>Domain/</c> resolves patterns
/// by id today; add the seam when a consumer needs it).
/// </summary>
public sealed class RhythmPatternStore
{
    private readonly ChordFlowDbContext _db;

    public RhythmPatternStore(ChordFlowDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <summary>Find a stored pattern by id and parse it into a <see cref="RhythmPattern"/>, or null if absent.</summary>
    public RhythmPattern? Find(string id)
    {
        Entities.RhythmPatternEntity? row = _db.RhythmPatterns.AsNoTracking().FirstOrDefault(p => p.Id == id);
        if (row is null)
        {
            return null;
        }

        var ts = new TimeSignature(row.TsNumerator, row.TsDenominator);
        return RhythmPatternParser.Parse(row.Id, row.Name, row.Dsl, ts);
    }
}

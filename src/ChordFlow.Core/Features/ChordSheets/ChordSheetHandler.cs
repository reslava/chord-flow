using ChordFlow.Bridge;
using ChordFlow.Features.Voicings;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using ChordFlow.Rendering.ChordSheets;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Features.ChordSheets;

/// <summary>
/// The one handler behind the <c>chordSheet</c> bridge verb (ChordSheetR) — the I/O seam peer of
/// <see cref="ExerciseRendering"/>. It resolves the harmony reference through the stores
/// (<see cref="ExerciseRefs"/>), expands it into a <see cref="RealizedSong"/> in the requested key, resolves a
/// <see cref="CompingPlan"/> only when the adornment needs fret diagrams, and hands both to the pure
/// <see cref="ChordSheetBuilder"/>. A short-lived <see cref="ChordFlowDbContext"/> per request, mirroring the
/// other DB-backed handlers.
/// </summary>
public sealed class ChordSheetHandler
{
    private static readonly Key CMajor = new(new PitchClass(0), IsMinor: false);

    private readonly DbContextOptions<ChordFlowDbContext> _dbOptions;

    public ChordSheetHandler(DbContextOptions<ChordFlowDbContext> dbOptions)
    {
        ArgumentNullException.ThrowIfNull(dbOptions);
        _dbOptions = dbOptions;
    }

    /// <summary>
    /// Build the sheet for <paramref name="req"/>. Throws (surfaced by the host as a <c>chordSheetError</c>) when
    /// the harmony reference can't be resolved.
    /// </summary>
    public ChordSheetResultEnvelope Build(ChordSheetRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        using var db = new ChordFlowDbContext(_dbOptions);

        Key? keyOverride = req.KeyPitchClass is int pc
            ? new Key(new PitchClass(((pc % 12) + 12) % 12), IsMinor: false)
            : null;

        // A bare progression is lifted at the chosen key (or C); a stored Song keeps its own InitialKey and the
        // override, if any, re-anchors it — exactly as ExerciseRendering computes the base key.
        Song song = ExerciseRefs.ResolveHarmony(req.HarmonyEntity, req.HarmonyId, keyOverride ?? CMajor, db);
        Key baseKey = keyOverride ?? song.InitialKey;

        RealizedSong realized = SongExpander.Expand(song, new ProgressionStore(db), startKey: baseKey);

        CompingPlan? comping = NeedsDiagram(req.Adornment)
            ? CompingResolver.Resolve(
                realized,
                req.Voicing ?? VoicingSource.Default,
                StoredVoicingSource.From(new VoicingStore(db)),
                VoicingReferenceSource.From(new VoicingStore(db)))
            : null;

        int barsPerRow = req.BarsPerRow < 1 ? 4 : req.BarsPerRow;
        ChordSheet sheet = ChordSheetBuilder.Build(
            song, realized, baseKey, TimeSignature.FourFour, new ChordSheetOptions(barsPerRow), comping);

        return new ChordSheetResultEnvelope(sheet);
    }

    // Only the two diagram-bearing adornment modes need a comping voicing resolved; tones-only and none don't.
    private static bool NeedsDiagram(string? adornment) =>
        adornment is not null
        && (adornment.Equals("diagram", StringComparison.OrdinalIgnoreCase)
            || adornment.Equals("both", StringComparison.OrdinalIgnoreCase));
}

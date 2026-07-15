using ChordFlow.Bridge;
using ChordFlow.Exercises;
using ChordFlow.Features.Voicings;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
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

    // Fallback play-time tempo when the song declares none — a neutral mid-tempo, matching the progression seed.
    private const int FallbackTempo = 100;

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

        // Comping grips are now resolved ALWAYS — playback needs the actual notes, not just the diagram
        // adornment. The same plan feeds both the fret diagram (when the adornment is on) and the audio render,
        // so the drawn grip is exactly what sounds.
        CompingPlan comping = CompingResolver.Resolve(
            realized,
            req.Voicing ?? VoicingSource.Default,
            StoredVoicingSource.From(new VoicingStore(db)),
            VoicingReferenceSource.From(new VoicingStore(db)));

        int barsPerRow = req.BarsPerRow < 1 ? 4 : req.BarsPerRow;
        ChordSheetBuildResult built = ChordSheetBuilder.Build(
            song, realized, baseKey, TimeSignature.FourFour, new ChordSheetOptions(barsPerRow),
            NeedsDiagram(req.Adornment) ? comping : null);

        // Render playable alphaTex from the SAME realized song (design D1-a, so (bar,beat) aligns by
        // construction) with a neutral quarter-note comp (every beat is an attack, so a split bar's mid-bar
        // chord change lands on a real beat). One pass yields the tex + the alphaTab-aligned chord schedule.
        RenderResult render = new AlphaTexRenderer().Render(
            realized, SeedData.Quarters, song.DefaultTempo ?? FallbackTempo, Difficulty.Beginner, comping,
            song.DefaultFeel ?? TripletFeel.None);

        IReadOnlyList<CellScheduleEntry> cellSchedule = BuildCellSchedule(built.BarSchedule, render.Schedule);

        return new ChordSheetResultEnvelope(built.Sheet, cellSchedule, render.Tex);
    }

    // Overlay the render schedule's mid-bar chord onsets onto the builder's per-bar downbeats (approach A):
    // every bar keeps its downbeat entry (bar-level highlight, incl. % and sustained bars); a split bar gains
    // one entry per mid-bar chord change, mapped to its chord-segment index (1, 2, … in beat order — segment 0
    // is the downbeat). (bar,beat) come straight from the alphaTab-aligned render schedule.
    private static IReadOnlyList<CellScheduleEntry> BuildCellSchedule(
        IReadOnlyList<CellScheduleEntry> barSchedule, IReadOnlyList<ChordChange> renderSchedule)
    {
        var cellByBar = barSchedule.ToDictionary(e => e.Bar);
        var entries = new List<CellScheduleEntry>(barSchedule);

        foreach (var barChanges in renderSchedule.GroupBy(c => c.Bar))
        {
            if (!cellByBar.TryGetValue(barChanges.Key, out CellScheduleEntry? cell))
            {
                continue;
            }

            var midBar = barChanges.Where(c => c.Beat > 0).OrderBy(c => c.Beat).ToList();
            for (int j = 0; j < midBar.Count; j++)
            {
                entries.Add(new CellScheduleEntry(
                    barChanges.Key, midBar[j].Beat, cell.Section, cell.Row, cell.Cell, Chord: j + 1));
            }
        }

        return entries.OrderBy(e => e.Bar).ThenBy(e => e.Beat).ToList();
    }

    // Only the two diagram-bearing adornment modes draw a fret diagram; tones-only and none don't.
    private static bool NeedsDiagram(string? adornment) =>
        adornment is not null
        && (adornment.Equals("diagram", StringComparison.OrdinalIgnoreCase)
            || adornment.Equals("both", StringComparison.OrdinalIgnoreCase));
}

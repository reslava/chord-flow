using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Persistence;

namespace ChordFlow.Features;

/// <summary>
/// Resolves stored content references into the Domain objects an <see cref="Exercise"/> composes — the shared
/// "ids → Exercise pieces" seam used by both the generate slice (which knows the harmony kind explicitly) and
/// the library load path (which only has a stored harmony id). Reads through the per-use stores and <b>fails
/// loud</b> with a clear message when a referenced row is missing, so a dangling reference surfaces as a UI
/// status rather than a silent wrong render. This is the <c>ui/exercise-workbench</c> IN8 plumbing — wiring
/// chosen ids into the existing realization pipeline, no new engine capability (req EX7).
/// </summary>
public static class ExerciseRefs
{
    /// <summary>
    /// Resolve the harmony reference to a <see cref="Song"/> given an explicit kind discriminator (the generate
    /// path): <c>"progression"</c> → load the stored progression and lift it at <paramref name="liftKey"/> via
    /// <see cref="Song.OfProgression"/>; <c>"song"</c> → load the stored Song (its own <see cref="Song.InitialKey"/>).
    /// Throws on an unknown kind or a missing row.
    /// </summary>
    public static Song ResolveHarmony(string harmonyEntity, string harmonyId, Key liftKey, ChordFlowDbContext db)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(harmonyId);

        return harmonyEntity?.Trim().ToLowerInvariant() switch
        {
            "song" => new SongStore(db).Find(harmonyId)
                ?? throw new InvalidOperationException($"Song '{harmonyId}' not found."),
            "progression" => Song.OfProgression(
                new ProgressionStore(db).Find(harmonyId)
                    ?? throw new InvalidOperationException($"Progression '{harmonyId}' not found."),
                liftKey),
            _ => throw new FormatException(
                $"Unknown harmony entity '{harmonyEntity}' (expected 'song' or 'progression')."),
        };
    }

    /// <summary>
    /// Resolve a harmony id with no stored kind discriminator (the saved-exercise <b>load</b> path, whose
    /// <c>ExerciseEntity.SongId</c> may name either a Song or a lifted bare Progression): try the Song store
    /// first, else fall back to a Progression lifted at <paramref name="liftKey"/>. Throws if neither resolves.
    /// </summary>
    public static Song ResolveHarmonyById(string harmonyId, Key liftKey, ChordFlowDbContext db)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(harmonyId);

        Song? song = new SongStore(db).Find(harmonyId);
        if (song is not null)
        {
            return song;
        }

        Progression? progression = new ProgressionStore(db).Find(harmonyId);
        if (progression is not null)
        {
            return Song.OfProgression(progression, liftKey);
        }

        throw new InvalidOperationException($"No song or progression found for harmony id '{harmonyId}'.");
    }

    /// <summary>Resolve a required rhythm-pattern reference; throws if the row is missing.</summary>
    public static RhythmPattern ResolvePattern(string patternId, ChordFlowDbContext db)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patternId);

        return new RhythmPatternStore(db).Find(patternId)
            ?? throw new InvalidOperationException($"Rhythm pattern '{patternId}' not found.");
    }

    /// <summary>Resolve an optional rhythm-pattern reference (null/blank id → no lead track).</summary>
    public static RhythmPattern? ResolveOptionalPattern(string? patternId, ChordFlowDbContext db) =>
        string.IsNullOrWhiteSpace(patternId) ? null : ResolvePattern(patternId, db);

    /// <summary>
    /// Resolve an optional drum-groove reference (<c>drums-under-a-song</c> IN8): null/blank id → no drum part;
    /// a non-blank id that resolves → the groove; a non-blank id that is missing → fail loud (a dangling
    /// reference surfaces as a UI status, mirroring the harmony/pattern resolvers).
    /// </summary>
    public static DrumGroove? ResolveDrumGroove(string? drumGrooveId, ChordFlowDbContext db) =>
        string.IsNullOrWhiteSpace(drumGrooveId)
            ? null
            : new DrumGrooveStore(db).Find(drumGrooveId)
                ?? throw new InvalidOperationException($"Drum groove '{drumGrooveId}' not found.");
}

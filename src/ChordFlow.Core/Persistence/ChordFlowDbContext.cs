using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// EF Core context over the local SQLite file (constraint C2: a single offline file,
/// no server, no network). Holds exercise <b>definitions</b> and practice events only;
/// alphaTex is never persisted. EF Core was chosen over Dapper for its migration tooling.
/// </summary>
public sealed class ChordFlowDbContext : DbContext
{
    public ChordFlowDbContext(DbContextOptions<ChordFlowDbContext> options) : base(options)
    {
    }

    public DbSet<ExerciseEntity> Exercises => Set<ExerciseEntity>();

    public DbSet<PracticeRecordEntity> PracticeRecords => Set<PracticeRecordEntity>();

    public DbSet<ProgressionEntity> Progressions => Set<ProgressionEntity>();

    public DbSet<SongEntity> Songs => Set<SongEntity>();

    public DbSet<RhythmPatternEntity> RhythmPatterns => Set<RhythmPatternEntity>();

    public DbSet<VoicingEntity> Voicings => Set<VoicingEntity>();

    /// <summary>
    /// Default on-disk database path: <c>%LOCALAPPDATA%\ChordFlow\chordflow.db</c>.
    /// Lives in the user profile (survives rebuilds), not next to the executable.
    /// Ensures the directory exists.
    /// </summary>
    public static string DefaultDbPath()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChordFlow");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "chordflow.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExerciseEntity>(e =>
        {
            e.HasKey(x => x.Id);
            // Store the enum by name (Beginner/Intermediate/Advanced) — readable in the DB
            // and stable if numeric values shift later.
            e.Property(x => x.Difficulty).HasConversion<string>();
            e.HasMany(x => x.PracticeRecords)
                .WithOne(r => r.Exercise!)
                .HasForeignKey(r => r.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PracticeRecordEntity>(e =>
        {
            e.HasKey(x => x.Id);
        });

        // Composite PK (Id, Origin) on every content entity: a definition's BuiltIn / Pack / UserDefined
        // copies physically coexist as separate rows (design §6.1 / D2). Lookups resolve the highest tier
        // per Id via OriginResolver, so shadowing is non-destructive — deleting a local copy lets the next
        // tier down win on the next resolve (IN3). "No duplicates" (IN5) means no two rows of the same
        // (Id, Origin): a re-import upserts the same-tier row.
        modelBuilder.Entity<ProgressionEntity>(e =>
        {
            e.HasKey(x => new { x.Id, x.Origin });
            // Store Origin by name (BuiltIn/UserDefined/Pack) — readable in the DB, matching the Difficulty convention.
            e.Property(x => x.Origin).HasConversion<string>();
            // Tags is a JSON array (constraint C3); default to an empty array so legacy/blank rows are well-formed.
            e.Property(x => x.Tags).HasDefaultValue("[]");
        });

        modelBuilder.Entity<SongEntity>(e =>
        {
            // Field-for-field parity with ProgressionEntity: composite (Id, Origin) PK, Origin by name, JSON-array Tags default.
            e.HasKey(x => new { x.Id, x.Origin });
            e.Property(x => x.Origin).HasConversion<string>();
            e.Property(x => x.Tags).HasDefaultValue("[]");
        });

        modelBuilder.Entity<RhythmPatternEntity>(e =>
        {
            // Composite (Id, Origin) PK + Origin by name. No catalog columns (EX3) — rhythm patterns aren't
            // genre-filtered; the TsNumerator/TsDenominator pair stores the meter.
            e.HasKey(x => new { x.Id, x.Origin });
            e.Property(x => x.Origin).HasConversion<string>();
        });

        modelBuilder.Entity<VoicingEntity>(e =>
        {
            // Field-for-field parity with ProgressionEntity: composite (Id, Origin) PK, Origin by name, JSON-array Tags default.
            e.HasKey(x => new { x.Id, x.Origin });
            e.Property(x => x.Origin).HasConversion<string>();
            e.Property(x => x.Tags).HasDefaultValue("[]");
        });
    }
}

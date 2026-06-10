using ChordFlow.Domain;
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

    /// <summary>
    /// First-run seeding: insert any <see cref="SeedData.BuiltInProgressions"/> not already present
    /// (matched by <c>Id</c>) with <see cref="ProgressionOrigin.BuiltIn"/>. Idempotent — re-running adds
    /// only missing rows and never touches existing or user-defined ones. Returns the number inserted.
    /// </summary>
    public int SeedBuiltInProgressions()
    {
        HashSet<string> existing = Progressions.Select(p => p.Id).ToHashSet();

        int added = 0;
        foreach (ProgressionDefinition def in SeedData.BuiltInProgressions)
        {
            if (existing.Contains(def.Id))
            {
                continue;
            }

            Progressions.Add(new ProgressionEntity
            {
                Id = def.Id,
                Name = def.Name,
                Dsl = def.Dsl,
                Origin = ProgressionOrigin.BuiltIn,
                CreatedUtc = DateTime.UtcNow,
            });
            added++;
        }

        if (added > 0)
        {
            SaveChanges();
        }

        return added;
    }

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

        modelBuilder.Entity<ProgressionEntity>(e =>
        {
            // Stable string id (slug for built-ins, GUID for user progressions) is the PK.
            e.HasKey(x => x.Id);
            // Store Origin by name (BuiltIn/UserDefined) — readable in the DB, matching the Difficulty convention.
            e.Property(x => x.Origin).HasConversion<string>();
        });
    }
}

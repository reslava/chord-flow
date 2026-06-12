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

    public DbSet<SongEntity> Songs => Set<SongEntity>();

    /// <summary>
    /// First-run seeding: insert any <see cref="SeedData.BuiltInProgressions"/> not already present
    /// (matched by <c>Id</c>) with <see cref="Origin.BuiltIn"/>. Idempotent — re-running adds
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

            // Denormalize any catalog header on the definition's DSL into the filter columns; the DSL
            // stays the canonical source. Header-less built-ins yield null genre/subgenre and an empty tag set.
            (CatalogMetadata meta, _) = CatalogHeader.Parse(def.Dsl);
            Progressions.Add(new ProgressionEntity
            {
                Id = def.Id,
                Name = def.Name,
                Dsl = def.Dsl,
                Origin = Origin.BuiltIn,
                Genre = meta.Genre,
                Subgenre = meta.Subgenre,
                Tags = CatalogHeader.SerializeTags(meta.Tags),
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
    /// First-run seeding of built-in songs: insert any <see cref="SeedData.BuiltInSongs"/> not already present
    /// (matched by <c>Id</c>) with <see cref="Origin.BuiltIn"/>, denormalizing each DSL's catalog header into the
    /// filter columns. Idempotent — re-running adds only missing rows and never touches existing or user songs.
    /// Returns the number inserted. Mirrors <see cref="SeedBuiltInProgressions"/>.
    /// </summary>
    public int SeedBuiltInSongs()
    {
        HashSet<string> existing = Songs.Select(s => s.Id).ToHashSet();

        int added = 0;
        foreach (SongDefinition def in SeedData.BuiltInSongs)
        {
            if (existing.Contains(def.Id))
            {
                continue;
            }

            (CatalogMetadata meta, _) = CatalogHeader.Parse(def.Dsl);
            Songs.Add(new SongEntity
            {
                Id = def.Id,
                Name = def.Name,
                Dsl = def.Dsl,
                Origin = Origin.BuiltIn,
                Genre = meta.Genre,
                Subgenre = meta.Subgenre,
                Tags = CatalogHeader.SerializeTags(meta.Tags),
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
            // Tags is a JSON array (constraint C3); default to an empty array so legacy/blank rows are well-formed.
            e.Property(x => x.Tags).HasDefaultValue("[]");
        });

        modelBuilder.Entity<SongEntity>(e =>
        {
            // Field-for-field parity with ProgressionEntity: string PK, Origin by name, JSON-array Tags default.
            e.HasKey(x => x.Id);
            e.Property(x => x.Origin).HasConversion<string>();
            e.Property(x => x.Tags).HasDefaultValue("[]");
        });
    }
}

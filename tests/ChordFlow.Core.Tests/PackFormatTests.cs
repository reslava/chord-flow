using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Step 4 — the pack bundle format (IN4, C4, C5): manifest parsing, per-file identity (filename = id +
/// optional <c>name:</c> header, design §6.4), and the directory reader walking per-kind folders (mixed packs).
/// </summary>
public class PackFormatTests
{
    // ---- PackManifest.Parse ----

    [Fact]
    public void Manifest_Parse_ReadsAllFields()
    {
        const string json = """
            {
              "id": "blues-essentials",
              "name": "Blues Essentials",
              "version": "1.2.0",
              "kind": "content",
              "provenance": "ChordFlow",
              "requires": ["core-theory"]
            }
            """;

        PackManifest m = PackManifest.Parse(json);

        Assert.Equal("blues-essentials", m.Id);
        Assert.Equal("Blues Essentials", m.Name);
        Assert.Equal("1.2.0", m.Version);
        Assert.Equal("content", m.Kind);
        Assert.Equal("ChordFlow", m.Provenance);
        Assert.Equal(new[] { "core-theory" }, m.Requires);
    }

    [Fact]
    public void Manifest_Parse_DefaultsOptionalFields()
    {
        // Only id present: name falls back to id, kind to "content", requires to empty, version to 0.0.0.
        PackManifest m = PackManifest.Parse("""{ "id": "minimal" }""");

        Assert.Equal("minimal", m.Id);
        Assert.Equal("minimal", m.Name);
        Assert.Equal("0.0.0", m.Version);
        Assert.Equal(PackManifest.ContentKindLabel, m.Kind);
        Assert.Empty(m.Requires);
    }

    [Fact]
    public void Manifest_Parse_MissingId_Throws()
    {
        Assert.Throws<FormatException>(() => PackManifest.Parse("""{ "name": "no id" }"""));
    }

    [Fact]
    public void Manifest_Parse_MalformedJson_Throws()
    {
        Assert.Throws<FormatException>(() => PackManifest.Parse("{ not json"));
    }

    // ---- PackDefinitionFile.Read ----

    [Fact]
    public void File_FilenameStemIsId()
    {
        PackDefinition def = PackDefinitionFile.Read(ContentKind.Progression, "12bar_blues.dsl", "17 17 17 17");
        Assert.Equal("12bar_blues", def.Id);
        Assert.Equal(ContentKind.Progression, def.Kind);
    }

    [Fact]
    public void File_NameHeader_IsPeeledFromDsl_AndCatalogHeaderStays()
    {
        string text = "name: 12-Bar Blues\ngenre: Blues\ntags: [12-bar]\n17 17 17 17 47 47 17 17 57 47 17 57";

        PackDefinition def = PackDefinitionFile.Read(ContentKind.Progression, "12bar_blues.dsl", text);

        Assert.Equal("12-Bar Blues", def.Name);
        // The name line is gone; the catalog header survives so the importer can denormalize it.
        Assert.DoesNotContain("name:", def.Dsl);
        Assert.Equal("genre: Blues\ntags: [12-bar]\n17 17 17 17 47 47 17 17 57 47 17 57", def.Dsl);

        (CatalogMetadata meta, string body) = CatalogHeader.Parse(def.Dsl);
        Assert.Equal("Blues", meta.Genre);
        Assert.Equal(new[] { "12-bar" }, meta.Tags);
        Assert.Equal("17 17 17 17 47 47 17 17 57 47 17 57", body);
    }

    [Fact]
    public void File_NameHeader_FoundAfterCatalogLines()
    {
        // `name:` need not be first — it is recognized anywhere in the leading header block.
        string text = "genre: Blues\nname: Beat 1\nX...............";
        PackDefinition def = PackDefinitionFile.Read(ContentKind.Rhythm, "beat_1.dsl", text);

        Assert.Equal("Beat 1", def.Name);
        Assert.Equal("genre: Blues\nX...............", def.Dsl);
    }

    [Fact]
    public void File_NoNameHeader_TitleCasesId()
    {
        PackDefinition def = PackDefinitionFile.Read(ContentKind.Rhythm, "beat_1.dsl", "X...............");
        Assert.Equal("Beat 1", def.Name);
        Assert.Equal("X...............", def.Dsl);
    }

    [Fact]
    public void File_EmptyNameValue_FallsBackToTitleCasedId()
    {
        PackDefinition def = PackDefinitionFile.Read(ContentKind.Song, "blues_song.dsl", "name:\n17 47 17 17");
        Assert.Equal("Blues Song", def.Name);
        Assert.Equal("17 47 17 17", def.Dsl);
    }

    // ---- PackReader.ReadFromDirectory ----

    [Fact]
    public void Reader_ReadsMixedPack()
    {
        using var pack = TempPack.WithManifest("blues-essentials");
        pack.Write("progressions/12bar_blues.dsl", "name: 12-Bar Blues\n17 17 17 17 47 47 17 17 57 47 17 57");
        pack.Write("rhythms/beat_1.dsl", "name: Beat 1\nX...............");
        pack.Write("songs/blues_song.dsl", "name: Blues Song\nverse: 12bar_blues\nverse");

        ContentPack loaded = PackReader.ReadFromDirectory(pack.Dir);

        Assert.Equal("blues-essentials", loaded.Manifest.Id);
        Assert.Equal(3, loaded.Definitions.Count);
        Assert.Contains(loaded.Definitions, d => d.Kind == ContentKind.Progression && d.Id == "12bar_blues");
        Assert.Contains(loaded.Definitions, d => d.Kind == ContentKind.Rhythm && d.Id == "beat_1");
        Assert.Contains(loaded.Definitions, d => d.Kind == ContentKind.Song && d.Id == "blues_song");
    }

    [Fact]
    public void Reader_SkipsAbsentKindFolders()
    {
        using var pack = TempPack.WithManifest("only-progressions");
        pack.Write("progressions/a.dsl", "1 4 5");

        ContentPack loaded = PackReader.ReadFromDirectory(pack.Dir);

        Assert.Single(loaded.Definitions);
        Assert.Equal(ContentKind.Progression, loaded.Definitions[0].Kind);
    }

    [Fact]
    public void Reader_MissingManifest_Throws()
    {
        using var pack = TempPack.WithoutManifest();
        pack.Write("progressions/a.dsl", "1 4 5");
        Assert.Throws<FileNotFoundException>(() => PackReader.ReadFromDirectory(pack.Dir));
    }

    [Fact]
    public void Reader_UnsupportedKind_Throws()
    {
        using var pack = TempPack.WithManifest("themepack", kind: "theme");
        Assert.Throws<NotSupportedException>(() => PackReader.ReadFromDirectory(pack.Dir));
    }

    [Fact]
    public void Reader_MissingDirectory_Throws()
    {
        string missing = Path.Combine(Path.GetTempPath(), "chordflow-no-such-pack-" + Guid.NewGuid().ToString("N"));
        Assert.Throws<DirectoryNotFoundException>(() => PackReader.ReadFromDirectory(missing));
    }

    // A throwaway pack directory under the temp folder.
    private sealed class TempPack : IDisposable
    {
        public string Dir { get; }

        private TempPack()
        {
            Dir = Path.Combine(Path.GetTempPath(), "chordflow-pack-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Dir);
        }

        /// <summary>A pack directory with a <c>manifest.json</c> (id + kind).</summary>
        public static TempPack WithManifest(string id, string kind = "content")
        {
            var pack = new TempPack();
            File.WriteAllText(
                Path.Combine(pack.Dir, PackReader.ManifestFileName),
                $$"""{ "id": "{{id}}", "kind": "{{kind}}" }""");
            return pack;
        }

        /// <summary>A pack directory with no <c>manifest.json</c> at all.</summary>
        public static TempPack WithoutManifest() => new();

        public void Write(string relativePath, string content)
        {
            string full = Path.Combine(Dir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Dir, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }
}

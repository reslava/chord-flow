using System.Collections.Generic;
using System.Linq;
using ChordFlow.Domain;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The built-in seed songs (<see cref="SeedData.BuiltInSongs"/>) are valid end to end — each parses, expands
/// against the seed progressions, and renders without error — proving the curated example is well-formed
/// without needing a database (the DB seeding path is covered by <see cref="SongPersistenceTests"/>).
/// </summary>
public class SongSeedTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    // An in-memory store over the built-in progressions, so stored references (verse: 12bar_blues) resolve.
    private sealed class SeedProgressionStore : IProgressionStore
    {
        private readonly Dictionary<string, Progression> _byId =
            SeedData.BuiltInProgressions.ToDictionary(
                d => d.Id,
                d => ProgressionParser.Parse(d.Id, d.Name, CatalogHeader.Parse(d.Dsl).Body, Ts));

        public Progression? Find(string id) => _byId.TryGetValue(id, out Progression? p) ? p : null;
    }

    [Fact]
    public void EveryBuiltInSong_Parses_Expands_AndRenders()
    {
        var store = new SeedProgressionStore();
        var renderer = new AlphaTexRenderer();

        Assert.NotEmpty(SeedData.BuiltInSongs);
        foreach (SongDefinition def in SeedData.BuiltInSongs)
        {
            (_, string body) = CatalogHeader.Parse(def.Dsl);
            Song song = SongParser.Parse(def.Id, def.Name, body, Ts);
            RealizedSong realized = SongExpander.Expand(song, store);

            Assert.NotEmpty(realized.Sections);
            string tex = renderer.Render(realized, SeedData.Beat1And3, 100, Difficulty.Beginner);
            Assert.StartsWith("\\title", tex);
        }
    }

    [Fact]
    public void BluesDemo_HasExpectedStructureAndModulation()
    {
        SongDefinition def = SeedData.BuiltInSongs.Single(s => s.Id == "blues_song_demo");
        (_, string body) = CatalogHeader.Parse(def.Dsl);
        Song song = SongParser.Parse(def.Id, def.Name, body, Ts);

        RealizedSong realized = SongExpander.Expand(song, new SeedProgressionStore());

        // intro, verse, verse, chorus, verse — 5 sections; the verse reference is the 12-bar blues.
        Assert.Equal(5, realized.Sections.Count);
        Assert.Contains(realized.Sections, s => s.Label == "verse" && s.Bars.Count == 12);

        // mod V lands the post-modulation sections in G (tonic 7).
        Assert.Contains(realized.Sections, s => s.Key.Tonic.Value == 7);
    }
}

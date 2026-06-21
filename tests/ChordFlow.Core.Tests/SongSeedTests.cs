using ChordFlow.Exercises;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using System.Collections.Generic;
using System.Linq;
using ChordFlow.Features.Packs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The default-pack songs are valid end to end — each parses, expands against the pack's progressions, and
/// renders without error — proving the curated example is well-formed without a database (the DB import path
/// is covered by <see cref="SongPersistenceTests"/>).
/// </summary>
public class SongSeedTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static IReadOnlyList<PackDefinition> SongsIn(ContentKind kind) =>
        DefaultPack.Load().Definitions.Where(d => d.Kind == kind).ToList();

    // An in-memory store over the default pack's progressions, so stored references (verse: 12bar_blues) resolve.
    private sealed class DefaultPackProgressionStore : IProgressionStore
    {
        private readonly Dictionary<string, Progression> _byId =
            DefaultPack.Load().Definitions
                .Where(d => d.Kind == ContentKind.Progression)
                .ToDictionary(
                    d => d.Id,
                    d => ProgressionParser.Parse(d.Id, d.Name, CatalogHeader.Parse(d.Dsl).Body, Ts));

        public Progression? Find(string id) => _byId.TryGetValue(id, out Progression? p) ? p : null;
    }

    [Fact]
    public void EveryDefaultSong_Parses_Expands_AndRenders()
    {
        var store = new DefaultPackProgressionStore();
        var renderer = new AlphaTexRenderer();

        IReadOnlyList<PackDefinition> songs = SongsIn(ContentKind.Song);
        Assert.NotEmpty(songs);
        foreach (PackDefinition def in songs)
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
        PackDefinition def = SongsIn(ContentKind.Song).Single(s => s.Id == "blues_song_demo");
        (_, string body) = CatalogHeader.Parse(def.Dsl);
        Song song = SongParser.Parse(def.Id, def.Name, body, Ts);

        RealizedSong realized = SongExpander.Expand(song, new DefaultPackProgressionStore());

        // intro, verse, verse, chorus, verse — 5 sections; the verse reference is the 12-bar blues.
        Assert.Equal(5, realized.Sections.Count);
        Assert.Contains(realized.Sections, s => s.Label == "verse" && s.Bars.Count == 12);

        // mod V lands the post-modulation sections in G (tonic 7).
        Assert.Contains(realized.Sections, s => s.Key.Tonic.Value == 7);
    }
}

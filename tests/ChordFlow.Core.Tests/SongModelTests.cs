using System;
using System.Collections.Generic;
using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The Song domain model: <see cref="Modulation.Apply"/> arithmetic and the guarded
/// <see cref="Song.FromSections"/> factory (every guard rejects, the happy path constructs).
/// </summary>
public class SongModelTests
{
    private static readonly Key CMajor = new(new PitchClass(0), false);

    private static Part Inline(string name) =>
        new InlineProgression(name, ProgressionParser.Parse(name, name, "1 4 5 1", TimeSignature.FourFour));

    // --- Modulation.Apply ---

    [Fact]
    public void Apply_UpAFifth_FromCMajor_IsGMajor()
    {
        Key g = new Modulation(7, null).Apply(CMajor);

        Assert.Equal(new PitchClass(7), g.Tonic);
        Assert.False(g.IsMinor);
    }

    [Fact]
    public void Apply_WrapsMod12()
    {
        // C (0) + 17 semitones = 17 → 5 (F).
        Key f = new Modulation(17, null).Apply(CMajor);
        Assert.Equal(new PitchClass(5), f.Tonic);

        // C (0) - 2 semitones = -2 → 10 (Bb).
        Key bb = new Modulation(-2, null).Apply(CMajor);
        Assert.Equal(new PitchClass(10), bb.Tonic);
    }

    [Fact]
    public void Apply_ModeChange_FlipsIsMinor_OtherwisePreservesIt()
    {
        Key relMinor = new Modulation(9, true).Apply(CMajor);   // C → A, to minor
        Assert.Equal(new PitchClass(9), relMinor.Tonic);
        Assert.True(relMinor.IsMinor);

        Key stillMajor = new Modulation(2, null).Apply(CMajor); // no mode flip
        Assert.False(stillMajor.IsMinor);
    }

    [Fact]
    public void Apply_AccumulatesWhenFoldedTwice()
    {
        var mod = new Modulation(7, null);
        Key twoFifths = mod.Apply(mod.Apply(CMajor));   // C → G → D

        Assert.Equal(new PitchClass(2), twoFifths.Tonic);
    }

    // --- Song.FromSections happy path ---

    [Fact]
    public void FromSections_ValidArrangement_Constructs()
    {
        var parts = new Dictionary<string, Part> { ["A"] = Inline("A"), ["B"] = Inline("B") };
        var items = new ArrangementItem[]
        {
            new PartPlay("A", 2),
            new RelativeMod(new Modulation(7, null)),
            new PartPlay("B", 1),
        };

        Song song = Song.FromSections("s1", "Test Song", CMajor, parts, items);

        Assert.Equal("s1", song.Id);
        Assert.Equal(CMajor, song.InitialKey);
        Assert.Equal(3, song.Items.Count);
    }

    // --- Guards ---

    [Fact]
    public void FromSections_UnknownPartPlay_Throws()
    {
        var parts = new Dictionary<string, Part> { ["A"] = Inline("A") };
        var items = new ArrangementItem[] { new PartPlay("missing", 1) };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => Song.FromSections("s", "S", CMajor, parts, items));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void FromSections_RepeatBelowOne_Throws()
    {
        var parts = new Dictionary<string, Part> { ["A"] = Inline("A") };
        var items = new ArrangementItem[] { new PartPlay("A", 0) };

        Assert.Throws<ArgumentException>(() => Song.FromSections("s", "S", CMajor, parts, items));
    }

    [Fact]
    public void FromSections_NoPartPlay_Throws()
    {
        var parts = new Dictionary<string, Part> { ["A"] = Inline("A") };
        var items = new ArrangementItem[] { new RelativeMod(new Modulation(7, null)) };

        Assert.Throws<ArgumentException>(() => Song.FromSections("s", "S", CMajor, parts, items));
    }

    [Fact]
    public void FromSections_ReferenceWithEmptyId_Throws()
    {
        var parts = new Dictionary<string, Part> { ["verse"] = new ProgressionReference("verse", "") };
        var items = new ArrangementItem[] { new PartPlay("verse", 1) };

        Assert.Throws<ArgumentException>(() => Song.FromSections("s", "S", CMajor, parts, items));
    }
}

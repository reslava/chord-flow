using ChordFlow.Features.ChordSheets;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using ChordFlow.Rendering;
using ChordFlow.Rendering.ChordSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChordFlow.Core.Tests.ChordSheets;

/// <summary>
/// <see cref="ChordSheetBuilder"/>: the pure projection of a realized song into the sheet model — row chunking,
/// per-chord notations (concrete / Nashville / Roman), the tone strip, multi-chord cell splitting, the derived
/// <c>%</c> simile, header metadata, and the optional diagram adornment.
/// </summary>
public class ChordSheetBuilderTests
{
    private static readonly Key CMajor = new(new PitchClass(0), false);
    private static readonly TimeSignature Ts = TimeSignature.FourFour;
    private static readonly ChordSheetOptions FourPerRow = new(BarsPerRow: 4);

    private static RealizedSong Realize(string dsl, Key key, string label = "A")
    {
        Progression prog = ProgressionParser.Parse("p", "P", dsl, Ts);
        return new RealizedSong(new[] { new RealizedSection(label, key, Transposer.RealizeBars(prog, key)) });
    }

    private static ChordSheet Build(string dsl, ChordSheetOptions? opts = null, CompingPlan? comping = null) =>
        BuildResult(dsl, opts, comping).Sheet;

    private static ChordSheetBuildResult BuildResult(string dsl, ChordSheetOptions? opts = null, CompingPlan? comping = null)
    {
        RealizedSong realized = Realize(dsl, CMajor);
        Song song = Song.OfProgression(ProgressionParser.Parse("p", "Blues", dsl, Ts), CMajor);
        return ChordSheetBuilder.Build(song, realized, CMajor, Ts, opts ?? FourPerRow, comping);
    }

    [Fact]
    public void Build_ChunksBarsIntoRowsOfBarsPerRow()
    {
        // Six single-chord bars at 4/row → a 4-cell row and a 2-cell row.
        ChordSheet sheet = Build("1 4 5 1 6- 5");

        ChordSheetSection section = Assert.Single(sheet.Sections);
        Assert.Equal("A", section.Label);
        Assert.Collection(
            section.Rows,
            row => Assert.Equal(4, row.Cells.Count),
            row => Assert.Equal(2, row.Cells.Count));
    }

    [Fact]
    public void Build_ChordRef_CarriesAllThreeNotations()
    {
        // ii m7 in C = Dm7 · Nashville "2-7" · Roman "ii7".
        ChordRef chord = Build("2-7").Sections[0].Rows[0].Cells[0].Chords.Single();

        Assert.Equal("Dm7", chord.Concrete);
        Assert.Equal("2-7", chord.Degree);
        Assert.Equal("ii7", chord.Roman);
    }

    [Theory]
    [InlineData("57", "G7", "57", "V7")]      // dominant
    [InlineData("1maj7", "Cmaj7", "1maj7", "Imaj7")]
    [InlineData("7dim7", "Bdim7", "7dim7", "vii°7")]
    [InlineData("#4dim7", "F#dim7", "#4dim7", "#iv°7")]  // chromatic degree
    public void Build_NotationTable(string dsl, string concrete, string degree, string roman)
    {
        ChordRef chord = Build(dsl).Sections[0].Rows[0].Cells[0].Chords.Single();
        Assert.Equal(concrete, chord.Concrete);
        Assert.Equal(degree, chord.Degree);
        Assert.Equal(roman, chord.Roman);
    }

    [Fact]
    public void Build_ToneStrip_IsSpelledAndFunctioned()
    {
        // Dm7 tones: D(root) F(third) A(fifth) C(seventh).
        IReadOnlyList<ChordSheetTone> tones = Build("2-7").Sections[0].Rows[0].Cells[0].Chords.Single().Tones;

        Assert.Equal(new[] { "D", "F", "A", "C" }, tones.Select(t => t.Note));
        Assert.Equal(new[] { "root", "third", "fifth", "seventh" }, tones.Select(t => t.Function));
        Assert.Equal("R", tones[0].Interval);
        Assert.Contains("b3", tones.Select(t => t.Interval));
        Assert.Contains("b7", tones.Select(t => t.Interval));
    }

    [Fact]
    public void Build_RepeatedBar_BecomesSimile()
    {
        // Two identical C-major bars: the second is a "%" (empty chords, RepeatOfPrev).
        IReadOnlyList<ChordSheetCell> cells = Build("1 1").Sections[0].Rows[0].Cells;

        Assert.False(cells[0].RepeatOfPrev);
        Assert.NotEmpty(cells[0].Chords);
        Assert.True(cells[1].RepeatOfPrev);
        Assert.Empty(cells[1].Chords);
    }

    [Fact]
    public void Build_DifferentAdjacentBars_AreNotSimiles()
    {
        IReadOnlyList<ChordSheetCell> cells = Build("1 4").Sections[0].Rows[0].Cells;
        Assert.False(cells[0].RepeatOfPrev);
        Assert.False(cells[1].RepeatOfPrev);
    }

    [Fact]
    public void Build_MultiChordBar_SplitsByDuration()
    {
        // One bar, two even-split chords (I7 · IV7) → one cell with two chords, 96 ticks each, bar 192.
        ChordSheetCell cell = Build("17_47").Sections[0].Rows[0].Cells.Single();

        Assert.Equal(192, cell.BarTicks);
        Assert.Collection(
            cell.Chords,
            c => { Assert.Equal("C7", c.Concrete); Assert.Equal(96, c.DurationTicks); },
            c => { Assert.Equal("F7", c.Concrete); Assert.Equal(96, c.DurationTicks); });
    }

    [Fact]
    public void Build_Header_CarriesSongMetadata()
    {
        Song song = SongParser.Parse("s", "My Blues", "capo 3\ntempo 120\nfeel triplet8th\nA = 1 4 5 1\nA", Ts);
        RealizedSong realized = Realize("1 4 5 1", CMajor);

        ChordSheetHeader header = ChordSheetBuilder.Build(song, realized, CMajor, Ts, FourPerRow).Sheet.Header;

        Assert.Equal("My Blues", header.Title);
        Assert.Null(header.Artist);
        Assert.Equal("C", header.KeyName);
        Assert.Equal(120, header.Tempo);
        Assert.Equal("Triplet8th", header.Feel);
        Assert.Equal("4/4", header.TimeSig);
        Assert.Equal(3, header.Capo);
    }

    [Fact]
    public void Build_KeyName_ReflectsSheetKey()
    {
        var eFlat = new Key(new PitchClass(3), false);
        RealizedSong realized = Realize("1 4 5 1", eFlat);
        Song song = Song.OfProgression(ProgressionParser.Parse("p", "P", "1 4 5 1", Ts), eFlat);

        ChordSheet sheet = ChordSheetBuilder.Build(song, realized, eFlat, Ts, FourPerRow).Sheet;
        Assert.Equal("Eb", sheet.Header.KeyName);
        Assert.Equal("Eb", sheet.Sections[0].Rows[0].Cells[0].Chords.Single().Concrete); // I in Eb = Eb
    }

    [Fact]
    public void Build_NoComping_LeavesDiagramNull()
    {
        ChordRef chord = Build("1").Sections[0].Rows[0].Cells[0].Chords.Single();
        Assert.Null(chord.Diagram);
    }

    [Fact]
    public void Build_WithComping_FillsDiagram()
    {
        RealizedSong realized = Realize("1", CMajor);
        Song song = Song.OfProgression(ProgressionParser.Parse("p", "P", "1", Ts), CMajor);

        // A trivial one-note grip for every realized chord so the diagram producer has something to draw.
        var grips = realized.Sections
            .SelectMany(s => s.Bars).SelectMany(b => b.Spans)
            .ToDictionary(span => span.Chord, _ => new Voicing(new[] { new FretPosition(6, 3) }, null, null, null));
        var comping = new CompingPlan(grips);

        ChordRef chord = ChordSheetBuilder.Build(song, realized, CMajor, Ts, FourPerRow, comping)
            .Sheet.Sections[0].Rows[0].Cells[0].Chords.Single();

        Assert.NotNull(chord.Diagram);
        Assert.Equal("C", chord.Diagram!.Title);
    }

    [Fact]
    public void Build_ZeroBarsPerRow_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build("1", new ChordSheetOptions(BarsPerRow: 0)));
    }

    [Fact]
    public void Build_BarSchedule_OneDownbeatEntryPerBar_CoveringSimiles()
    {
        // 6 bars incl. a % (bar 1 repeats bar 0), 4/row → row0 = bars 0..3 (cells 0..3), row1 = bars 4,5 (cells 0,1).
        IReadOnlyList<CellScheduleEntry> schedule = BuildResult("1 1 4 5 6- 5").BarSchedule;

        // One entry per bar, sequential global bar index, all at the downbeat + chord 0 of a single section.
        Assert.Equal(6, schedule.Count);
        Assert.Equal(Enumerable.Range(0, 6), schedule.Select(e => e.Bar));
        Assert.All(schedule, e => { Assert.Equal(0, e.Beat); Assert.Equal(0, e.Chord); Assert.Equal(0, e.Section); });

        // The % bar (bar 1) still gets its own entry, at (row 0, cell 1).
        Assert.Equal(new CellScheduleEntry(1, 0, 0, 0, 1, 0), schedule[1]);
        // Row/cell mapping follows the 4/row chunking: bar 4 → row 1 cell 0, bar 5 → row 1 cell 1.
        Assert.Equal(new CellScheduleEntry(4, 0, 0, 1, 0, 0), schedule[4]);
        Assert.Equal(new CellScheduleEntry(5, 0, 0, 1, 1, 0), schedule[5]);
    }

    [Fact]
    public void Build_BarSchedule_SplitBar_HasSingleDownbeatEntry()
    {
        // A split bar (two chords) still emits ONE downbeat entry (chord 0); the per-chord onsets are the
        // handler's overlay job, not the builder's (approach A — the builder has no rhythm-slot layout).
        CellScheduleEntry entry = Assert.Single(BuildResult("17_47").BarSchedule);
        Assert.Equal(new CellScheduleEntry(0, 0, 0, 0, 0, 0), entry);
    }
}

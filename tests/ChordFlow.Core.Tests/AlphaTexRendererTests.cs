using ChordFlow.Exercises;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Songs;
using ChordFlow.Rendering;
using ChordFlow.Instruments.Drums;
using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

public class AlphaTexRendererTests
{
    private static readonly AlphaTexRenderer Renderer = new();
    private static readonly Key Bb = new(new PitchClass(10), false); // Bb major

    [Fact]
    public void Render_KnownExercise_ProducesExpectedAlphaTex()
    {
        // One-bar "I" progression in Bb, Beat 1 rhythm — a single strike ringing the whole bar (a whole
        // note, sustain-literal), tempo 80.
        var progression = new Progression("test", "Test Blues", new RomanDegree[] { new(1, Quality.Dominant7) });

        string tex = Renderer.RenderProgression(Bb, progression, SeedData.Beat1, 80, Difficulty.Beginner);

        string expected = string.Join("\n",
            "\\title \"Test Blues — Bb\"",
            "\\subtitle \"Beginner — Beat 1\"",
            "\\tempo 80",
            "\\ts 4 4",
            "\\ks bb",
            ".",
            ":1 (1.5 0.4 1.3) |");

        Assert.Equal(expected, tex);
    }

    [Fact]
    public void Render_FullBbBlues_HasTwelveBarsAndCorrectHeader()
    {
        string tex = Renderer.RenderProgression(Bb, SeedData.TwelveBarBlues, SeedData.Beat1And3, 80, Difficulty.Beginner);

        Assert.StartsWith("\\title \"12-Bar Blues — Bb\"", tex);
        Assert.Contains("\\subtitle \"Beginner — Beats 1 & 3\"", tex);
        Assert.Contains("\\ks bb", tex);
        Assert.Contains("\\ts 4 4", tex);

        // 12 bars => 12 pipe separators.
        Assert.Equal(12, tex.Count(c => c == '|'));

        // Beats 1 & 3 ring as two half notes, so the stateful duration is ":2", emitted exactly once.
        Assert.Equal(1, CountOccurrences(tex, ":2"));

        // I = Bb7, IV = Eb7, V = F7 voicings all present.
        Assert.Contains("(1.5 0.4 1.3)", tex); // Bb7
        Assert.Contains("(6.5 5.4 6.3)", tex); // Eb7
        Assert.Contains("(8.5 7.4 8.3)", tex); // F7
    }

    [Fact]
    public void Render_QuartersRhythm_EmitsFourHitsPerBar()
    {
        var progression = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });

        string tex = Renderer.RenderProgression(Bb, progression, SeedData.Quarters, 90, Difficulty.Beginner);

        Assert.EndsWith(":4 (1.5 0.4 1.3) (1.5 0.4 1.3) (1.5 0.4 1.3) (1.5 0.4 1.3) |", tex);
        Assert.DoesNotContain("r", tex.Split('\n')[^1]); // no rests in the bar line
    }

    [Fact]
    public void Render_TickPatternWithCustomTimeSignatureHeader_DerivesTsFromPattern()
    {
        // The \ts header now derives from the pattern's TimeSignature rather than a hardcoded "4 4".
        var progression = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });

        string tex = Renderer.RenderProgression(Bb, progression, SeedData.Quarters, 90, Difficulty.Beginner);

        Assert.Contains("\\ts 4 4", tex);
        // Quantized through the new tick path: four quarter hits, stateful ":4" once.
        Assert.EndsWith(":4 (1.5 0.4 1.3) (1.5 0.4 1.3) (1.5 0.4 1.3) (1.5 0.4 1.3) |", tex);
    }

    [Fact]
    public void Render_Pickup_EmitsLeadingMeasureBeforeBars()
    {
        // A one-beat pickup voiced with the first chord adds a leading bar (=> an extra pipe).
        var pickup = new PickupMeasure(new[] { RhythmEvent.Hit(0, 48) }, LengthTicks: 48);
        var rhythm = RhythmPattern.SingleBar("p", "Pickup", SeedData.Beat1.Bars[0].Events, TimeSignature.FourFour, pickup);
        var progression = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });

        string tex = Renderer.RenderProgression(Bb, progression, rhythm, 80, Difficulty.Beginner);

        // Pickup bar + the single progression bar = 2 pipes (\ac is a prefix, not a new bar). The pickup
        // is marked an anacrusis and carries the ":4"; the main bar is Beat 1 ringing the whole bar → ":1".
        Assert.Equal(2, tex.Count(c => c == '|'));
        Assert.EndsWith("\\ac :4 (1.5 0.4 1.3) |\n:1 (1.5 0.4 1.3) |", tex);
    }

    [Fact]
    public void Render_EighthTriplets_EmitsTuTokenOnEverySlot()
    {
        var rhythm = RhythmPatternParser.Parse("trip", "Triplets", ":3 XXX XXX XXX XXX", TimeSignature.FourFour);
        var prog = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });

        string tex = Renderer.RenderProgression(Bb, prog, rhythm, 90, Difficulty.Beginner);

        // Twelve tupled eighths, stateful ":8" once, "{tu 3}" on each slot (it does not persist).
        string inner = string.Join(" ", Enumerable.Repeat("(1.5 0.4 1.3){tu 3}", 12));
        Assert.EndsWith(":8 " + inner + " |", tex);
        Assert.Equal(12, CountOccurrences(tex, "{tu 3}"));
    }

    [Fact]
    public void Render_PerBeatMixedGrid_InterleavesStraightAndTupletTokens()
    {
        var rhythm = RhythmPatternParser.Parse("mix", "Mixed", "XXX:3 X... X.X:3 X...", TimeSignature.FourFour);
        var prog = new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });

        string tex = Renderer.RenderProgression(Bb, prog, rhythm, 90, Difficulty.Beginner);

        Assert.EndsWith(
            ":8 (1.5 0.4 1.3){tu 3} (1.5 0.4 1.3){tu 3} (1.5 0.4 1.3){tu 3} " +
            ":4 (1.5 0.4 1.3) (1.5 0.4 1.3){tu 3} :8 (1.5 0.4 1.3){tu 3} :4 (1.5 0.4 1.3) |",
            tex);
    }

    [Fact]
    public void Render_MinorKey_EmitsMinorKeySignatureAndSpelledChordNames()
    {
        // first-class-minor-keys (C), end-to-end through the renderer: a minor tune authored tonic-relative
        // (`tonality: minor`) emits `\ks aminor`, spells its chords from the relative major, and spells the
        // harmonic vii°7 raised root letter-pure — G♯dim7, not A♭dim7. The old MVP-only minor guard is gone.
        var aMinor = new Key(new PitchClass(9), IsMinor: true);
        Progression prog = ProgressionParser.Parse(
            "t", "A minor tune", "1- 4- 5- #7dim7", TimeSignature.FourFour, home: Tonality.Minor);

        string tex = Renderer.RenderProgression(aMinor, prog, SeedData.Beat1, 80, Difficulty.Beginner,
            options: new RenderOptions(ShowChordNames: true));

        Assert.Contains("\\ks aminor", tex);
        Assert.Contains("{ch \"Am\"}", tex);
        Assert.Contains("{ch \"Dm\"}", tex);
        Assert.Contains("{ch \"Em\"}", tex);
        Assert.Contains("{ch \"G#dim7\"}", tex); // raised leading tone spelled G♯, not A♭
    }

    [Fact]
    public void Render_TwoChordBar_VoicesEachHalfWithItsChord()
    {
        // "17_67" = I7 (first half) · VI7 (second half) in Bb, struck on every quarter.
        var prog = ProgressionParser.Parse("p", "P", "17_67", TimeSignature.FourFour);

        IReadOnlyList<string> groups = ChordGroups(LastBar(
            Renderer.RenderProgression(Bb, prog, SeedData.Quarters, 90, Difficulty.Beginner)));

        Assert.Equal(4, groups.Count);
        Assert.Equal(groups[0], groups[1]);     // both quarters of the I7 half
        Assert.Equal(groups[2], groups[3]);     // both quarters of the VI7 half
        Assert.NotEqual(groups[0], groups[2]);  // the chord actually changes at the boundary
    }

    [Fact]
    public void Render_ThreeChordBar_ExplicitSlots_VoicesNinetySixFortyEightFortyEight()
    {
        // "17:2_67:1_27:1" = I7 (half) · VI7 (quarter) · ii7 (quarter), struck on every quarter.
        var prog = ProgressionParser.Parse("p", "P", "17:2_67:1_27:1", TimeSignature.FourFour);

        IReadOnlyList<string> groups = ChordGroups(LastBar(
            Renderer.RenderProgression(Bb, prog, SeedData.Quarters, 90, Difficulty.Beginner)));

        Assert.Equal(4, groups.Count);
        Assert.Equal(groups[0], groups[1]);     // the I7 half spans quarters 1–2
        Assert.NotEqual(groups[1], groups[2]);  // → VI7 at quarter 3
        Assert.NotEqual(groups[2], groups[3]);  // → ii7 at quarter 4
        Assert.NotEqual(groups[0], groups[3]);
    }

    [Fact]
    public void Render_FourChordBar_VoicesEachQuarterDistinctly()
    {
        var prog = ProgressionParser.Parse("p", "P", "17_27_37_47", TimeSignature.FourFour);

        IReadOnlyList<string> groups = ChordGroups(LastBar(
            Renderer.RenderProgression(Bb, prog, SeedData.Quarters, 90, Difficulty.Beginner)));

        Assert.Equal(4, groups.Count);
        Assert.Equal(4, groups.Distinct().Count()); // I7/II7/III7/IV7 all different
    }

    [Fact]
    public void Render_BluesViaDsl_IsByteIdenticalToSeedProgression()
    {
        // The DSL round-trip must reproduce the existing seed output exactly (C4 backward compatibility).
        string viaSeed = Renderer.RenderProgression(Bb, SeedData.TwelveBarBlues, SeedData.Beat1And3, 80, Difficulty.Beginner);

        var dslProg = ProgressionParser.Parse(
            "12bar_blues", "12-Bar Blues", "17 17 17 17 47 47 17 17 57 47 17 57", TimeSignature.FourFour);
        string viaDsl = Renderer.RenderProgression(Bb, dslProg, SeedData.Beat1And3, 80, Difficulty.Beginner);

        Assert.Equal(viaSeed, viaDsl);
    }

    [Fact]
    public void Render_NullOptions_MatchesDefaultOptions()
    {
        Assert.Equal(BbI7QuartersTex(), BbI7QuartersTex(RenderOptions.Default));
    }

    [Fact]
    public void Render_ShowChordNames_AttachesLabelOncePerChordChange_AndSuppressesDiagrams()
    {
        string tex = BbI7QuartersTex(new RenderOptions(ShowChordNames: true));

        // Names without the fret boxes: the directive is emitted explicitly as false.
        Assert.Contains("\\chordDiagramsInScore false", tex);
        // The label is attached at the chord change only — not on every strum of the same chord.
        Assert.Equal(1, CountOccurrences(tex, "{ch \"Bb7\"}"));
        Assert.EndsWith(
            ":4 (1.5 0.4 1.3){ch \"Bb7\"} (1.5 0.4 1.3) (1.5 0.4 1.3) (1.5 0.4 1.3) |", tex);
    }

    [Fact]
    public void Render_ShowChordNames_LabelsEachDistinctChordChange()
    {
        var prog = ProgressionParser.Parse("p", "P", "17_27_37_47", TimeSignature.FourFour);

        string tex = Renderer.RenderProgression(Bb, prog, SeedData.Quarters, 90, Difficulty.Beginner,
            options: new RenderOptions(ShowChordNames: true));

        Assert.Equal(4, CountOccurrences(tex, "{ch \""));
    }

    [Fact]
    public void Render_ShowChordDiagramsOverStaff_DefinesDiagramInHeaderAndEnablesOverStaffOnly()
    {
        string tex = BbI7QuartersTex(new RenderOptions(ShowChordDiagramsOverStaff: true));

        // Over-staff on → the only chord-diagram alphaTex directive, \chordDiagramsInScore (bare = show).
        // On-top has no alphaTex directive (it's the JS stylesheet flag), so it never appears in the tex.
        Assert.Contains("\\chordDiagramsInScore\n", tex);
        Assert.DoesNotContain("chordDiagramsOnTop", tex);
        // \chord def: frets string 1 (high E) → string 6 (low E), x for an unplayed string; defined once,
        // in the metadata header (before the lone "."); the beat references it by name with {ch "…"}.
        Assert.Equal(1, CountOccurrences(tex, "\\chord (\"Bb7\" x x 1 0 1 x)"));
        int defIndex = tex.IndexOf("\\chord (\"Bb7\"", System.StringComparison.Ordinal);
        int dotIndex = tex.IndexOf("\n.\n", System.StringComparison.Ordinal);
        Assert.True(defIndex >= 0 && defIndex < dotIndex, "\\chord definition must precede the metadata terminator");
        Assert.Contains("(1.5 0.4 1.3){ch \"Bb7\"}", tex);
        Assert.DoesNotContain("\\chord (\"Bb7\" x x 1 0 1 x) (1.5", tex); // not inline
    }

    [Fact]
    public void Render_ShowChordDiagramsOnTop_EmitsDefsAndSuppressesOverStaff_NoOnTopDirective()
    {
        string tex = BbI7QuartersTex(new RenderOptions(ShowChordDiagramsOnTop: true));

        // On-top has no alphaTex directive — the top list is driven by \chord defs + the JS stylesheet flag.
        Assert.DoesNotContain("chordDiagramsOnTop", tex);
        Assert.Contains("\\chordDiagramsInScore false\n", tex);   // over-staff stays off
        Assert.Equal(1, CountOccurrences(tex, "\\chord (\"Bb7\" x x 1 0 1 x)")); // defs feed the top list
        Assert.Contains("(1.5 0.4 1.3){ch \"Bb7\"}", tex);
    }

    [Fact]
    public void ChordDefinition_GripUpTheNeck_EmitsFirstFret()
    {
        // A high grip (firstfret 6) must carry {firstfret 6} so alphaTab doesn't draw the box from the nut
        // with the dots floating in the air (engine grips up the neck made this visible).
        var prog = new Progression("t", "T", new RomanDegree[] { new(1, Quality.Dominant7) });
        var realized = new RealizedSong(new[] { new RealizedSection("t", Bb, Transposer.RealizeBars(prog, Bb)) });
        Chord chord = realized.Sections[0].Bars[0].Spans[0].Chord;
        var highGrip = new Voicing(
            new[] { new FretPosition(5, 6), new FretPosition(4, 8), new FretPosition(3, 7) }, FirstFret: 6);
        var plan = new CompingPlan(new Dictionary<Chord, Voicing> { [chord] = highGrip });

        string tex = Renderer.Render(
            realized, SeedData.Beat1, 80, Difficulty.Beginner, plan,
            options: new RenderOptions(ShowChordDiagramsOnTop: true)).Tex;

        Assert.Contains("{firstfret 6}", tex);
    }

    [Fact]
    public void Render_WithLead_EmitsTwoTracksWithDeadNoteLead()
    {
        // Comping = Beat 1 (a whole-note strike); Lead = quarters → four dead notes on string 3.
        RealizedSong song = OneBarI7();

        string tex = Renderer.Render(song, SeedData.Beat1, 80, Difficulty.Beginner, lead: SeedData.Quarters).Tex;

        // Score metadata + the lone "." precede the first \track (\ts/\ks moved into each track). Bars-per-row
        // is a JS display setting now, so no `{ defaultSystemsLayout }` block on the \track line.
        Assert.Contains("\\track \"Comping\" \"comp\"\n", tex);
        Assert.Contains("\\track \"Lead\" \"lead\"\n", tex);
        Assert.Equal(2, CountOccurrences(tex, "\\ts 4 4")); // one per track
        Assert.Equal(2, CountOccurrences(tex, "\\ks bb"));
        Assert.Equal(1, CountOccurrences(tex, "\n.\n"));    // exactly one metadata terminator
        // Comping bar unchanged; lead bar is the rhythm rendered as dead notes.
        Assert.Contains(":1 (1.5 0.4 1.3) |", tex);
        Assert.Contains(":4 x.3 x.3 x.3 x.3 |", tex);
        // Two master bars total (one per track), so two pipe separators.
        Assert.Equal(2, tex.Count(c => c == '|'));
    }

    [Fact]
    public void Render_WithoutLead_HasNoTrackWrapper()
    {
        // Single-track output must stay byte-identical to the pre-lead renderer (design §7.4) — no \track.
        string tex = Renderer.RenderProgression(Bb, I7Progression(), SeedData.Beat1, 80, Difficulty.Beginner);

        Assert.DoesNotContain("\\track", tex);
        // Guard: a render with no pickup never emits an anacrusis marker on a regular section bar (C3).
        Assert.DoesNotContain("\\ac", tex);
    }

    [Fact]
    public void Render_WithLeadAndPickup_MirrorsPickupAsRestsOnLeadTrack()
    {
        // A comping pickup gets a matching leading bar on the lead track — but as a rest (the lead doesn't
        // play during the anacrusis in v1), so the staves stay bar-aligned.
        var pickup = new PickupMeasure(new[] { RhythmEvent.Hit(0, 48) }, LengthTicks: 48);
        var rhythm = RhythmPattern.SingleBar("p", "Pickup", SeedData.Beat1.Bars[0].Events, TimeSignature.FourFour, pickup);

        string tex = Renderer.Render(OneBarI7(), rhythm, 80, Difficulty.Beginner, lead: SeedData.Quarters).Tex;

        // Pickup bar + one main bar, on each of the two tracks → 4 pipes.
        Assert.Equal(4, tex.Count(c => c == '|'));
        string leadSection = tex[tex.IndexOf("\\track \"Lead\"", System.StringComparison.Ordinal)..];
        Assert.Contains("\\ac :4 r |", leadSection); // the lead pickup is a rest, marked as an anacrusis
    }

    [Fact]
    public void Render_WithDrums_EmitsPercussionTrack()
    {
        // Comping alone + a drum groove ⇒ multi-track: a Comping guitar staff + a Drums percussion staff.
        string tex = Renderer.Render(OneBarI7(), SeedData.Beat1, 80, Difficulty.Beginner, drums: RockGroove()).Tex;

        Assert.Contains("\\track \"Comping\" \"comp\"\n", tex);
        Assert.Contains("\\track \"Drums\" \"dr\"\n", tex);
        Assert.Contains("\\instrument percussion\n", tex);
        Assert.Contains("\\articulation defaults\n", tex);
        Assert.Equal(2, CountOccurrences(tex, "\\ts 4 4")); // comping + drums, one \ts each

        // Percussion is keyless: the \ks stays on the comping track only, never on the drum staff.
        string drumSection = tex[tex.IndexOf("\\track \"Drums\"", System.StringComparison.Ordinal)..];
        Assert.DoesNotContain("\\ks", drumSection);
        Assert.Contains("kickhit", drumSection);
        Assert.Contains("hihatclosed", drumSection);
    }

    [Fact]
    public void Render_Drums_TileCyclicallyAcrossTheSongBars()
    {
        // A 2-bar groove under a 12-bar blues tiles to 12 drum bars (bar i → groove bar i % 2, IN3), staying
        // bar-aligned with the 12-bar comping staff.
        var song = new RealizedSong(new[]
        {
            new RealizedSection(SeedData.TwelveBarBlues.Name, Bb, Transposer.RealizeBars(SeedData.TwelveBarBlues, Bb)),
        });

        string tex = Renderer.Render(song, SeedData.Beat1And3, 80, Difficulty.Beginner, drums: TwoBarGroove()).Tex;

        string drumSection = tex[tex.IndexOf("\\track \"Drums\"", System.StringComparison.Ordinal)..];
        Assert.Equal(12, drumSection.Count(c => c == '|')); // 12 tiled drum bars
        Assert.Equal(24, tex.Count(c => c == '|'));          // 12 comping + 12 drums
    }

    [Fact]
    public void Render_WithoutDrums_HasNoPercussionTrack()
    {
        // Guard: no drum part ⇒ no percussion staff at all (single-track stays byte-identical elsewhere).
        string tex = Renderer.RenderProgression(Bb, I7Progression(), SeedData.Beat1, 80, Difficulty.Beginner);

        Assert.DoesNotContain("\\instrument percussion", tex);
        Assert.DoesNotContain("\\track \"Drums\"", tex);
    }

    [Fact]
    public void Render_Drums_RideTheSameWholeSongTripletFeel()
    {
        // Swing is one song-level \tf per track (IN6): the drum staff's first bar carries it too, so a swung
        // song swings comp AND drums together.
        string tex = Renderer.Render(
            OneBarI7(), SeedData.Beat1, 80, Difficulty.Beginner, tripletFeel: TripletFeel.Triplet8th,
            drums: RockGroove()).Tex;

        string drumSection = tex[tex.IndexOf("\\track \"Drums\"", System.StringComparison.Ordinal)..];
        // The \tf sits at the very start of the drum track's first bar (after the header lines).
        Assert.Contains("\\tf triplet8th ", drumSection);
    }

    [Fact]
    public void Render_Schedule_RecordsOneEntryPerChordChangeAcrossBars()
    {
        var realized = new RealizedSong(new[]
        {
            new RealizedSection(SeedData.TwelveBarBlues.Name, Bb, Transposer.RealizeBars(SeedData.TwelveBarBlues, Bb)),
        });

        RenderResult result = Renderer.Render(realized, SeedData.Beat1, 80, Difficulty.Beginner);

        // 12-bar blues in Bb (Bb7×4, Eb7×2, Bb7×2, F7, Eb7, Bb7, F7): one entry per *change*, each on beat 0.
        Assert.Equal(
            new[]
            {
                (0, 0, "Bb7"), (4, 0, "Eb7"), (6, 0, "Bb7"),
                (8, 0, "F7"), (9, 0, "Eb7"), (10, 0, "Bb7"), (11, 0, "F7"),
            },
            result.Schedule.Select(c => (c.Bar, c.Beat, c.Name)).ToArray());

        // Each entry carries a real-root diagram matching the comped voicing: the first is the (1.5 0.4 1.3)
        // Bb7 shell the tab plays, titled "Bb7", root on the A string (proves fidelity + real-root anchoring).
        FretboardDiagram bb7 = result.Schedule[0].Diagram;
        Assert.Equal("Bb7", bb7.Title);
        Assert.Equal(
            new Dictionary<int, int> { { 5, 1 }, { 4, 0 }, { 3, 1 } },
            bb7.Markers.ToDictionary(m => m.String, m => m.Fret));
        Assert.Equal("R", bb7.Markers.Single(m => m.String == 5).Interval);
    }

    [Fact]
    public void Render_Schedule_InteriorChordChange_RecordsTheSecondChordAtItsBeatOrdinal()
    {
        // One bar, two chords (I7 then VI7), even split → VI7 starts at the half bar = beat 2 of a quarters grid.
        Progression prog = ProgressionParser.Parse("t", "Test", "17_67", TimeSignature.FourFour);
        var realized = new RealizedSong(new[]
        {
            new RealizedSection(prog.Name, Bb, Transposer.RealizeBars(prog, Bb)),
        });

        RenderResult result = Renderer.Render(realized, SeedData.Quarters, 80, Difficulty.Beginner);

        Assert.Equal(
            new[] { (0, 0, "Bb7"), (0, 2, "G7") },
            result.Schedule.Select(c => (c.Bar, c.Beat, c.Name)).ToArray());
    }

    [Fact]
    public void Render_Schedule_WithPickup_CountsTheAnacrusisAsBarZero()
    {
        // The bar-index contract the chord sheet relies on (sheet-pickup-bar D1): the \ac bar consumes
        // render bar 0 — the first chord is recorded THERE (the pickup sounds it), and every full bar
        // sits one higher, matching alphaTab's master bars. The ChordSheetBuilder counts its lead-in
        // cell the same way, so the two schedules stay on one axis.
        var pickup = new PickupMeasure(new[] { RhythmEvent.Hit(0, 48) }, LengthTicks: 48);
        var rhythm = RhythmPattern.SingleBar("p", "Pickup", SeedData.Quarters.Bars[0].Events, TimeSignature.FourFour, pickup);
        Progression prog = ProgressionParser.Parse("t", "Test", "17 47", TimeSignature.FourFour);
        var realized = new RealizedSong(new[]
        {
            new RealizedSection(prog.Name, Bb, Transposer.RealizeBars(prog, Bb)),
        });

        RenderResult result = Renderer.Render(realized, rhythm, 80, Difficulty.Beginner);

        // Bb7 lands in the pickup (bar 0); the IV7 change lands in the SECOND full bar = bar 2, not 1.
        Assert.Equal(
            new[] { (0, 0, "Bb7"), (2, 0, "Eb7") },
            result.Schedule.Select(c => (c.Bar, c.Beat, c.Name)).ToArray());
    }

    [Fact]
    public void Render_DottedNote_EmitsDotBeatEffect()
    {
        // ":2 X..X----" = the Charleston: a dotted quarter + an eighth (then rests), all on Bb7.
        string tex = Renderer.RenderProgression(
            Bb, I7Progression(), Pattern(":2 X..X----"), 80, Difficulty.Beginner);

        Assert.EndsWith(":4 (1.5 0.4 1.3){d} :8 (1.5 0.4 1.3) :2 r |", tex);
    }

    [Fact]
    public void Render_AuthoredTie_EmitsTieFretGroup()
    {
        // "X..._...X...X..." = a quarter tied to the next quarter, then two more quarters — the '_' tied
        // continuation re-states the held Bb7 strings with the tie fret "-".
        string tex = Renderer.RenderProgression(
            Bb, I7Progression(), Pattern("X..._...X...X..."), 80, Difficulty.Beginner);

        Assert.EndsWith(":4 (1.5 0.4 1.3) (-.5 -.4 -.3) (1.5 0.4 1.3) (1.5 0.4 1.3) |", tex);
    }

    [Fact]
    public void Render_CrossBarTie_HoldsPreviousChord_RhythmWinsOverHarmony()
    {
        // Bb7 (bar 1) → Eb7 (bar 2). A whole note in bar 1, then a leading '_' in bar 2 ties into it.
        // Rhythm wins over harmony: bar 2 HOLDS Bb7's strings (-.s) and never attacks the Eb7.
        Progression prog = ProgressionParser.Parse("t", "Test", "17 47", TimeSignature.FourFour);
        string tex = Renderer.RenderProgression(
            Bb, prog, Pattern("X...............|_...------------"), 80, Difficulty.Beginner);

        Assert.Contains("(-.5 -.4 -.3)", tex);       // bar 2 holds the tied Bb7
        Assert.DoesNotContain("(6.5 5.4 6.3)", tex); // Eb7 is never attacked — the tie overrides the change
    }

    private static RhythmPattern Pattern(string dsl) =>
        RhythmPatternParser.Parse("p", "P", dsl, TimeSignature.FourFour);

    private static Progression I7Progression() =>
        new("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) });

    private static RealizedSong OneBarI7() =>
        new(new[] { new RealizedSection("Test", Bb, Transposer.RealizeBars(I7Progression(), Bb)) });

    // A one-bar rock groove: eight hi-hat eighths + kick on beats 1 & 3 (48 PPQ, 4/4). Used for the
    // percussion-track shape + feel tests.
    private static DrumGroove RockGroove() => DrumGroove.SingleBar(
        "rock", "Rock",
        new[]
        {
            new DrumLane(DrumVoice.HiHatClosed, Enumerable.Range(0, 8).Select(i => RhythmEvent.Hit(i * 24, 24)).ToArray()),
            new DrumLane(DrumVoice.Kick, new[] { RhythmEvent.Hit(0, 48), RhythmEvent.Hit(96, 48) }),
        },
        TimeSignature.FourFour);

    // A 2-bar groove (kick bar, then hi-hat bar) for the cyclic-tiling test.
    private static DrumGroove TwoBarGroove() => new(
        "g2", "G2",
        new[]
        {
            new DrumBar(new[] { new DrumLane(DrumVoice.Kick, new[] { RhythmEvent.Hit(0, 48) }) }),
            new DrumBar(new[] { new DrumLane(DrumVoice.HiHatClosed, new[] { RhythmEvent.Hit(0, 48) }) }),
        },
        TimeSignature.FourFour);

    // A single Bb I7 bar struck on every quarter — the fixture for the RenderOptions tests.
    private static string BbI7QuartersTex(RenderOptions? options = null) =>
        Renderer.RenderProgression(
            Bb,
            new Progression("test", "Test", new RomanDegree[] { new(1, Quality.Dominant7) }),
            SeedData.Quarters, 90, Difficulty.Beginner, options: options);

    private static string LastBar(string tex) => tex.Split('\n')[^1];

    private static IReadOnlyList<string> ChordGroups(string barLine) =>
        System.Text.RegularExpressions.Regex.Matches(barLine, @"\([^)]*\)")
            .Select(m => m.Value)
            .ToList();

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}

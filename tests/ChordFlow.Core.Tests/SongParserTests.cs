using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using System;
using System.Linq;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// <see cref="SongParser"/>: the full DSL round-trip, each grammar form, the mod-spec table, the C-major
/// default, and grammar errors (undefined play, bad repeat, duplicate definition, malformed key).
/// </summary>
public class SongParserTests
{
    private static readonly TimeSignature Ts = TimeSignature.FourFour;

    private static Song Parse(string dsl) => SongParser.Parse("s", "Song", dsl, Ts);

    [Fact]
    public void Parse_FullSketch_ProducesPartsAndStream()
    {
        const string dsl = """
            key C

            A = 17 17 47 17        # inline local progression
            B = 2-7 57 1maj7
            C = 67 27 57 17

            A x2
            B x2
            mod V                   # modulate to the dominant
            C
            B x3
            """;

        Song song = Parse(dsl);

        // InitialKey is C major; three inline parts defined.
        Assert.Equal(new PitchClass(0), song.InitialKey.Tonic);
        Assert.False(song.InitialKey.IsMinor);
        Assert.Equal(3, song.Parts.Count);
        Assert.All(song.Parts.Values, p => Assert.IsType<InlineProgression>(p));

        // Stream order: A x2, B x2, mod V (+7), C x1, B x3.
        Assert.Collection(
            song.Items,
            i => Assert.Equal(("A", 2), AsPlay(i)),
            i => Assert.Equal(("B", 2), AsPlay(i)),
            i => Assert.Equal(7, Assert.IsType<RelativeMod>(i).Modulation.Semitones),
            i => Assert.Equal(("C", 1), AsPlay(i)),
            i => Assert.Equal(("B", 3), AsPlay(i)));
    }

    [Fact]
    public void Parse_NoKeyLine_DefaultsToCMajor()
    {
        Song song = Parse("A = 1 4 5 1\nA");

        Assert.Equal(new PitchClass(0), song.InitialKey.Tonic);
        Assert.False(song.InitialKey.IsMinor);
    }

    [Fact]
    public void Parse_ReferenceDefinition_BecomesProgressionReference()
    {
        Song song = Parse("verse: 12bar_blues\nverse x2");

        ProgressionReference reference = Assert.IsType<ProgressionReference>(song.Parts["verse"]);
        Assert.Equal("12bar_blues", reference.ProgressionId);
        Assert.Equal(("verse", 2), AsPlay(Assert.Single(song.Items)));
    }

    [Fact]
    public void Parse_AbsoluteKeyInStream_BecomesAbsoluteKeyReset()
    {
        Song song = Parse("A = 1 4 5 1\nA\nkey Eb\nA");

        // key C-default initial; "key Eb" mid-stream is an AbsoluteKey item, not the initial key.
        Assert.Equal(new PitchClass(0), song.InitialKey.Tonic);
        AbsoluteKey reset = Assert.IsType<AbsoluteKey>(song.Items[1]);
        Assert.Equal(new PitchClass(3), reset.Key.Tonic);   // Eb = 3
    }

    [Theory]
    [InlineData("+2", 2, null)]
    [InlineData("-3", -3, null)]
    [InlineData("V", 7, null)]
    [InlineData("IV", 5, null)]
    [InlineData("bIII", 3, null)]
    [InlineData("vi", 9, true)]
    public void Parse_ModSpecTable(string spec, int expectedSemitones, bool? expectedModeChange)
    {
        Song song = Parse($"A = 1 4 5 1\nA\nmod {spec}\nA");

        RelativeMod mod = Assert.IsType<RelativeMod>(song.Items[1]);
        Assert.Equal(expectedSemitones, mod.Modulation.Semitones);
        Assert.Equal(expectedModeChange, mod.Modulation.ModeChange);
    }

    [Fact]
    public void Parse_PlayWithoutRepeat_DefaultsToOne()
    {
        Song song = Parse("A = 1 4 5 1\nA");
        Assert.Equal(("A", 1), AsPlay(Assert.Single(song.Items)));
    }

    [Fact]
    public void Parse_UndefinedPlay_ThrowsFormat()
    {
        FormatException ex = Assert.Throws<FormatException>(() => Parse("A = 1 4 5 1\nB"));
        Assert.Contains("B", ex.Message);
    }

    [Fact]
    public void Parse_BadRepeat_ThrowsFormat()
    {
        Assert.Throws<FormatException>(() => Parse("A = 1 4 5 1\nA x0"));
        Assert.Throws<FormatException>(() => Parse("A = 1 4 5 1\nA z2"));
    }

    [Fact]
    public void Parse_DuplicateDefinition_ThrowsFormat()
    {
        Assert.Throws<FormatException>(() => Parse("A = 1 4 5 1\nA = 2 5 1 1\nA"));
    }

    [Fact]
    public void Parse_MalformedKey_ThrowsFormat()
    {
        Assert.Throws<FormatException>(() => Parse("key H\nA = 1 4 5 1\nA"));
    }

    // --- feel directive (Song.DefaultFeel) ---

    [Theory]
    [InlineData("none", TripletFeel.None)]
    [InlineData("triplet8th", TripletFeel.Triplet8th)]
    [InlineData("triplet16th", TripletFeel.Triplet16th)]
    [InlineData("Triplet8th", TripletFeel.Triplet8th)]   // idents are case-insensitive
    public void Parse_FeelDirective_SetsDefaultFeel(string token, TripletFeel expected)
    {
        Song song = Parse($"feel {token}\nA = 1 4 5 1\nA");
        Assert.Equal(expected, song.DefaultFeel);
    }

    [Fact]
    public void Parse_NoFeelDirective_DefaultFeelIsNull()
    {
        // Absent (no opinion) is distinct from an explicit `feel none` (req IN7).
        Song song = Parse("A = 1 4 5 1\nA");
        Assert.Null(song.DefaultFeel);
    }

    [Fact]
    public void Parse_FeelNone_IsNoneNotNull()
    {
        Song song = Parse("feel none\nA = 1 4 5 1\nA");
        Assert.Equal(TripletFeel.None, song.DefaultFeel);
        Assert.NotNull(song.DefaultFeel);
    }

    [Fact]
    public void Parse_FeelDirective_IsPositionIndependent()
    {
        // A whole-song directive: valid after the stream, not only before it.
        Song song = Parse("A = 1 4 5 1\nA\nfeel triplet8th");
        Assert.Equal(TripletFeel.Triplet8th, song.DefaultFeel);
    }

    [Fact]
    public void Parse_UnknownFeelToken_ThrowsFormat()
    {
        FormatException ex = Assert.Throws<FormatException>(() => Parse("feel swingish\nA = 1 4 5 1\nA"));
        Assert.Contains("swingish", ex.Message);
    }

    [Fact]
    public void Parse_DuplicateFeel_ThrowsFormat()
    {
        Assert.Throws<FormatException>(() => Parse("feel triplet8th\nfeel none\nA = 1 4 5 1\nA"));
    }

    [Fact]
    public void Parse_FeelAsPartName_ThrowsFormat()
    {
        // `feel` is a reserved keyword — it cannot name a part.
        Assert.Throws<FormatException>(() => Parse("feel = 1 4 5 1\nfeel"));
    }

    [Fact]
    public void Parse_FeelDirective_SurvivesTextualRoundTrip()
    {
        // No structural Song→DSL emitter exists; the DSL string is stored verbatim, so re-parsing the same
        // authored text preserves the feel (req IN6/C4 — this is what carries it across packs by construction).
        const string dsl = "feel triplet8th\nA = 1 4 5 1\nA";
        Assert.Equal(Parse(dsl).DefaultFeel, Parse(dsl).DefaultFeel);
        Assert.Equal(TripletFeel.Triplet8th, Parse(dsl).DefaultFeel);
    }

    // --- tempo directive (Song.DefaultTempo) ---

    [Theory]
    [InlineData("40", 40)]
    [InlineData("120", 120)]
    [InlineData("240", 240)]
    public void Parse_TempoDirective_SetsDefaultTempo(string token, int expected)
    {
        Song song = Parse($"tempo {token}\nA = 1 4 5 1\nA");
        Assert.Equal(expected, song.DefaultTempo);
    }

    [Fact]
    public void Parse_NoTempoDirective_DefaultTempoIsNull()
    {
        // Absent (no opinion) is distinct from any explicit value — the 80 default is applied downstream.
        Song song = Parse("A = 1 4 5 1\nA");
        Assert.Null(song.DefaultTempo);
    }

    [Fact]
    public void Parse_TempoDirective_IsPositionIndependent()
    {
        // A whole-song directive: valid after the stream, not only before it.
        Song song = Parse("A = 1 4 5 1\nA\ntempo 132");
        Assert.Equal(132, song.DefaultTempo);
    }

    [Theory]
    [InlineData("39")]     // below the 40–240 window
    [InlineData("241")]    // above it
    [InlineData("fast")]   // not an integer
    [InlineData("-120")]   // NumberStyles.None rejects the sign
    [InlineData("120.5")]  // not an integer
    public void Parse_OutOfRangeOrMalformedTempo_ThrowsFormat(string token)
    {
        Assert.Throws<FormatException>(() => Parse($"tempo {token}\nA = 1 4 5 1\nA"));
    }

    [Fact]
    public void Parse_DuplicateTempo_ThrowsFormat()
    {
        Assert.Throws<FormatException>(() => Parse("tempo 120\ntempo 90\nA = 1 4 5 1\nA"));
    }

    [Fact]
    public void Parse_TempoAsPartName_ThrowsFormat()
    {
        // `tempo` is a reserved keyword — it cannot name a part.
        Assert.Throws<FormatException>(() => Parse("tempo = 1 4 5 1\ntempo"));
    }

    // --- voice directive (Song.Voices) + inline annotations (IN4/C6, IN1/IN7) ---

    [Fact]
    public void Parse_VoiceDefaults_QualityAndDegreeScoped()
    {
        const string dsl = """
            voice *7 = 3 3 2 3 1 x
            voice 17 = 8 x 7 9 8 x
            voice #4dim7 = 8 x 7 8 7 x
            voice * = x 3 2 0 1 0
            A = 17 47 17 17
            A
            """;

        Song song = Parse(dsl);

        Assert.Equal("3 3 2 3 1 x", song.Voices[VoiceSelector.ForQuality(Quality.Dominant7)]);
        Assert.Equal("8 x 7 9 8 x", song.Voices[VoiceSelector.ForDegree(new RomanDegree(1, Quality.Dominant7))]);
        Assert.Equal("8 x 7 8 7 x", song.Voices[VoiceSelector.ForDegree(new RomanDegree(4, Quality.Diminished7, Accidental.Sharp))]);
        Assert.Equal("x 3 2 0 1 0", song.Voices[VoiceSelector.ForQuality(Quality.Major)]);
    }

    [Fact]
    public void Parse_VoiceDefault_KeepsReferenceSpecVerbatim()
    {
        Song song = Parse("voice *7 = u: C6\nA = 17 47 17 17\nA");
        Assert.Equal("u: C6", song.Voices[VoiceSelector.ForQuality(Quality.Dominant7)]);
    }

    [Fact]
    public void Parse_DuplicateVoiceSelector_ThrowsFormat()
    {
        Assert.Throws<FormatException>(() =>
            Parse("voice *7 = 3 3 2 3 1 x\nvoice *7 = 8 x 7 9 8 x\nA = 17 47 17 17\nA"));
    }

    [Theory]
    [InlineData("voice *zz = 3 3 2 3 1 x")]   // unknown quality
    [InlineData("voice 8 = 3 3 2 3 1 x")]     // degree out of range
    [InlineData("voice 17")]                  // no '=' / no spec
    [InlineData("voice = 3 3 2 3 1 x")]       // empty selector
    public void Parse_MalformedVoiceDirective_ThrowsFormat(string voiceLine)
    {
        Assert.Throws<FormatException>(() => Parse($"{voiceLine}\nA = 17 47 17 17\nA"));
    }

    [Fact]
    public void Parse_VoiceAsPartName_ThrowsFormat()
    {
        // `voice` is a reserved keyword — `voice=…` (no space) still can't name a part.
        Assert.Throws<FormatException>(() => Parse("voice=1 4 5 1\nvoice"));
    }

    [Fact]
    public void Parse_PartNameStartingWithVoice_IsNotADirective()
    {
        // `voiceleading` is a normal part name, not a `voice` directive.
        Song song = Parse("voiceleading = 1 4 5 1\nvoiceleading");
        Assert.IsType<InlineProgression>(song.Parts["voiceleading"]);
        Assert.Empty(song.Voices);
    }

    [Fact]
    public void Parse_InlineProgression_AcceptsPerChordAnnotation()
    {
        Song song = Parse("A = 17 {8 x 7 9 8 x} 47 17 17\nA");

        var inline = Assert.IsType<InlineProgression>(song.Parts["A"]);
        ChordSpan first = inline.Progression.Bars[0].Spans[0];
        Assert.Equal("8 x 7 9 8 x", first.VoicingAnnotation);
    }

    // --- Round-trip (IN8): the annotation spec writer + verbatim-text stability ---

    [Fact]
    public void AnnotatedSong_SurvivesTextualRoundTrip()
    {
        // A Song has no structural DSL emitter — its text is stored verbatim — so re-parsing the same authored
        // text must preserve the `voice` map and the inline `{…}` annotations (the round-trip by construction).
        const string dsl =
            "voice *7 = 3 3 2 3 1 x\nvoice #4dim7 = 8 x 7 8 7 x\nA = 17 {8 x 7 9 8 x root:6} 47 {u: C6}\nA";
        Song song = Parse(dsl);

        Assert.Equal("3 3 2 3 1 x", song.Voices[VoiceSelector.ForQuality(Quality.Dominant7)]);
        Assert.Equal(
            "8 x 7 8 7 x",
            song.Voices[VoiceSelector.ForDegree(new RomanDegree(4, Quality.Diminished7, Accidental.Sharp))]);

        var inline = Assert.IsType<InlineProgression>(song.Parts["A"]);
        Assert.Equal("8 x 7 9 8 x root:6", inline.Progression.Bars[0].Spans[0].VoicingAnnotation);
        Assert.Equal("u: C6", inline.Progression.Bars[1].Spans[0].VoicingAnnotation);
    }

    [Theory]
    [InlineData("8 x 7 9 8 x root:6")]   // grip + voiced anchor
    [InlineData("x 3 2 3 1 x root:6@8")] // rootless phantom anchor
    [InlineData("u: C6")]                // reference
    public void InlineAnnotationSpec_RoundTripsThroughTheWriter(string spec)
    {
        // The stored raw annotation feeds the voicing-spec writer and re-parses byte-for-byte — the actual
        // serialization surface for a `{…}` annotation (there being no whole-progression emitter).
        Song song = Parse($"A = 17 {{{spec}}}\nA");
        string raw = Assert.IsType<InlineProgression>(song.Parts["A"]).Progression.Bars[0].Spans[0].VoicingAnnotation!;

        Assert.Equal(spec, raw);
        Assert.Equal(spec, VoicingDslWriter.SpecToDsl(VoicingDslParser.ParseSpec(raw)));
    }

    private static (string Name, int Repeat) AsPlay(ArrangementItem item)
    {
        PartPlay play = Assert.IsType<PartPlay>(item);
        return (play.PartName, play.Repeat);
    }
}

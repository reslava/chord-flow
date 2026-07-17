using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Persistence;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The shared catalog-header mechanism (IN1, C1, C3): the optional <c>genre:</c>/<c>subgenre:</c>/<c>tags:</c>
/// block splits off the body, denormalizes to entity fields, round-trips 1:1, and never reaches the pure
/// <c>Domain/</c> bar parser.
/// </summary>
public class CatalogHeaderTests
{
    // first-class-minor-keys (C): a `tonality:` header key carries a progression's authoring frame; it rides
    // in the stored DSL and re-parses on load, so no dedicated column (like `description`).
    [Fact]
    public void Parse_TonalityMinor_IsExtracted_AndBodyUnchanged()
    {
        (CatalogMetadata meta, string body) = CatalogHeader.Parse("genre: Jazz\ntonality: minor\n1- 2ø 5 1-");

        Assert.Equal(Tonality.Minor, meta.Tonality);
        Assert.False(meta.IsEmpty);
        Assert.Equal("1- 2ø 5 1-", body);
    }

    [Fact]
    public void Tonality_RoundTripsThroughSerialize()
    {
        var meta = new CatalogMetadata("Jazz", null, Array.Empty<string>(), Tonality: Tonality.Minor);

        (CatalogMetadata back, string body) = CatalogHeader.Parse(CatalogHeader.Serialize(meta, "1- 2ø 5 1-"));

        Assert.Equal(Tonality.Minor, back.Tonality);
        Assert.Equal("1- 2ø 5 1-", body);
    }

    [Fact]
    public void Serialize_MajorTonality_EmitsNoTonalityLine()
    {
        Assert.Equal("17 47", CatalogHeader.Serialize(CatalogMetadata.Empty, "17 47"));
    }

    [Fact]
    public void Parse_UnknownTonality_Throws()
    {
        Assert.Throws<FormatException>(() => CatalogHeader.Parse("tonality: bogus\n1"));
    }

    [Fact]
    public void Parse_NoHeader_ReturnsEmptyMetadata_AndBodyUnchanged()
    {
        const string dsl = "17 17 17 17 47 47 17 17 57 47 17 57";

        (CatalogMetadata meta, string body) = CatalogHeader.Parse(dsl);

        Assert.True(meta.IsEmpty);
        Assert.Null(meta.Genre);
        Assert.Null(meta.Subgenre);
        Assert.Empty(meta.Tags);
        Assert.Equal(dsl, body);
    }

    [Fact]
    public void Parse_FullHeader_ExtractsMetadata_AndLeavesOnlyTheBody()
    {
        const string dsl = "genre: Blues\nsubgenre: Shuffle\ntags: [12-bar, beginner]\n17 17 47 17";

        (CatalogMetadata meta, string body) = CatalogHeader.Parse(dsl);

        Assert.Equal("Blues", meta.Genre);
        Assert.Equal("Shuffle", meta.Subgenre);
        Assert.Equal(new[] { "12-bar", "beginner" }, meta.Tags);
        Assert.Equal("17 17 47 17", body);
    }

    [Fact]
    public void Parse_StopsAtFirstNonHeaderLine_UnknownKeysAreBody()
    {
        // "foo: bar" is not a recognized header key, so it (and everything after) is body.
        const string dsl = "genre: Jazz\nfoo: bar\n2-7 57 17";

        (CatalogMetadata meta, string body) = CatalogHeader.Parse(dsl);

        Assert.Equal("Jazz", meta.Genre);
        Assert.Null(meta.Subgenre);
        Assert.Equal("foo: bar\n2-7 57 17", body);
    }

    [Theory]
    [InlineData("Blues", "Shuffle", new[] { "12-bar", "beginner" }, "17 17 47 17")]
    [InlineData("Jazz", null, new string[0], "2-7 57 17")]
    [InlineData(null, null, new[] { "solo" }, "1 4 5 1")]
    public void Serialize_ThenParse_RoundTripsExactly(string? genre, string? subgenre, string[] tags, string body)
    {
        var meta = new CatalogMetadata(genre, subgenre, tags);

        string dsl = CatalogHeader.Serialize(meta, body);
        (CatalogMetadata back, string parsedBody) = CatalogHeader.Parse(dsl);

        Assert.Equal(genre, back.Genre);
        Assert.Equal(subgenre, back.Subgenre);
        Assert.Equal(tags, back.Tags);
        Assert.Equal(body, parsedBody);
    }

    [Fact]
    public void Serialize_EmptyMetadata_ReturnsBodyUnchanged()
    {
        Assert.Equal("17 17 47 17", CatalogHeader.Serialize(CatalogMetadata.Empty, "17 17 47 17"));
    }

    [Fact]
    public void Parse_Description_IsExtracted_AndRoundTrips()
    {
        const string dsl = "genre: Jazz\ndescription: The fundamental jazz cadence.\ntags: [ii-V-I]\n2-7 57 1maj7";

        (CatalogMetadata meta, string body) = CatalogHeader.Parse(dsl);
        Assert.Equal("The fundamental jazz cadence.", meta.Description);
        Assert.Equal("2-7 57 1maj7", body);

        // Serialize → Parse round-trips the description (and never leaks into the body).
        (CatalogMetadata back, string parsedBody) = CatalogHeader.Parse(CatalogHeader.Serialize(meta, body));
        Assert.Equal("The fundamental jazz cadence.", back.Description);
        Assert.Equal("2-7 57 1maj7", parsedBody);
    }

    [Fact]
    public void Tags_JsonColumn_RoundTrips()
    {
        var tags = new[] { "12-bar", "beginner", "shuffle" };

        string json = CatalogHeader.SerializeTags(tags);
        IReadOnlyList<string> back = CatalogHeader.DeserializeTags(json);

        Assert.Equal(tags, back);
        Assert.Empty(CatalogHeader.DeserializeTags(null));
        Assert.Empty(CatalogHeader.DeserializeTags(""));
    }

    [Fact]
    public void HeaderBody_FeedsThePureDomainParser_WithoutMetadata()
    {
        // C1: the Domain bar parser never sees the header — realization strips it first.
        const string dsl = "genre: Blues\ntags: [12-bar]\n17 17 17 17 47 47 17 17 57 47 17 57";

        (_, string body) = CatalogHeader.Parse(dsl);
        Progression prog = ProgressionParser.Parse("x", "X", body, TimeSignature.FourFour);

        Assert.Equal(12, prog.Bars.Count);
    }
}

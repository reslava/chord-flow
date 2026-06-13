using ChordFlow.Domain;
using Xunit;

namespace ChordFlow.Core.Tests;

public class VoicingDslWriterTests
{
    [Theory]
    [InlineData("voicing Cmaj shape:C root:5 frets: x 3 2 0 1 0")]
    [InlineData("voicing C7 shape:E root:6 frets: 8 10 8 9 8 8")]
    [InlineData("voicing Cmin shape:A root:5 frets: x 3 1 0 1 3")]
    public void ToDsl_RoundTripsThroughParse(string canonical)
    {
        VoicingShape shape = VoicingDslParser.Parse(canonical);
        string dsl = VoicingDslWriter.ToDsl(shape);
        VoicingShape reparsed = VoicingDslParser.Parse(dsl);

        Assert.Equal(shape.Quality, reparsed.Quality);
        Assert.Equal(shape.Shape, reparsed.Shape);
        Assert.Equal(shape.RootString, reparsed.RootString);
        Assert.Equal(
            shape.Canonical.Positions.OrderBy(p => p.String),
            reparsed.Canonical.Positions.OrderBy(p => p.String));
        // ToDsl is idempotent — re-serializing the re-parsed shape yields the same line.
        Assert.Equal(dsl, VoicingDslWriter.ToDsl(reparsed));
    }

    [Fact]
    public void ToDsl_EmitsTheCanonicalCAnchor_NotTheAuthoredAnchor()
    {
        // Open G authored at G serializes as the canonical-C G-shape (875558), not back at G.
        VoicingShape shape = VoicingDslParser.Parse("voicing Gmaj shape:G root:6 frets: 3 2 0 0 0 3");

        Assert.Equal("voicing Cmaj shape:G root:6 frets: 8 7 5 5 5 8", VoicingDslWriter.ToDsl(shape));
    }
}

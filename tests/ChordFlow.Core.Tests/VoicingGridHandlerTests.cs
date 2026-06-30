using ChordFlow.Bridge;
using ChordFlow.Features.Voicings;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class VoicingGridHandlerTests
{
    private const int RootC = 0;

    private static readonly VoicingGridHandler Handler = new();

    // A bare {root} request (every level unconstrained) returns the whole realizable automatic grid.
    [Fact]
    public void Build_AllLevelsUnconstrained_ReturnsTheRealizableCatalogGrid()
    {
        VoicingGridResultEnvelope result = Handler.Build(Filter());

        Assert.NotEmpty(result.Cells);
        Assert.True(result.Cells.Count <= CagedVoicingCatalog.Combos.Count);

        // Every cell is a distinct, well-formed automatic catalog combo with a built diagram.
        Assert.Equal(result.Cells.Count, result.Cells.Select(c => c.Id).Distinct().Count());
        foreach (VoicingGridCell cell in result.Cells)
        {
            Assert.True(AutomaticVoicingId.TryParse(cell.Id, out VoicingFamily family, out Quality quality, out CagedShape shape));
            Assert.Contains((family, quality, shape), CagedVoicingCatalog.Combos);
            Assert.Equal(family.Token(), cell.Family);
            Assert.Equal(quality.ToString(), cell.Quality);
            Assert.Equal(shape.ToString(), cell.Shape);
            Assert.NotNull(cell.Diagram);
            Assert.NotEmpty(cell.Diagram.Markers);
        }
    }

    [Fact]
    public void Build_VoicingGridResult_HasTheResultType() =>
        Assert.Equal("voicingGridResult", Handler.Build(Filter()).Type);

    // Within a level → OR via set membership: only qualities whose 3rd is minor survive a thirds=["minor"] filter.
    [Fact]
    public void Build_ThirdFacetFilter_KeepsOnlyMatchingQualities()
    {
        VoicingGridResultEnvelope result = Handler.Build(Filter(thirds: ["minor"]));

        Assert.NotEmpty(result.Cells);
        Assert.All(result.Cells, c =>
            Assert.Equal(ThirdFacet.Minor, QualityFacets.Of(Enum.Parse<Quality>(c.Quality)).Third));
    }

    [Fact]
    public void Build_SeventhFacetFilter_KeepsOnlyMatchingColor()
    {
        VoicingGridResultEnvelope result = Handler.Build(Filter(sevenths: ["7"]));

        Assert.NotEmpty(result.Cells);
        Assert.All(result.Cells, c =>
            Assert.Equal("7", QualityFacets.Of(Enum.Parse<Quality>(c.Quality)).SeventhToken));
    }

    [Fact]
    public void Build_FamilyFilter_KeepsOnlyThatFamily()
    {
        VoicingGridResultEnvelope result = Handler.Build(Filter(families: ["shell"]));

        Assert.NotEmpty(result.Cells);
        Assert.All(result.Cells, c =>
        {
            Assert.Equal("shell", c.Family);
            Assert.StartsWith("auto:shell:", c.Id);
        });
    }

    // Across levels → AND: shell + major-3rd + perfect-5th + ♭7 isolates the dominant-7 shells (C and E forms).
    [Fact]
    public void Build_FullFacetIntersection_IsolatesDominant7Shells()
    {
        VoicingGridResultEnvelope result = Handler.Build(Filter(
            families: ["shell"], thirds: ["major"], fifths: ["perfect"], sevenths: ["7"]));

        Assert.All(result.Cells, c => Assert.Equal("Dominant7", c.Quality));
        Assert.Contains(result.Cells, c => c.Id == "auto:shell:dom7:C");
        Assert.Contains(result.Cells, c => c.Id == "auto:shell:dom7:E");
    }

    // m(maj7) is not in the catalog (minor + maj7) — a valid but empty intersection ⇒ empty grid, never an error (C5).
    [Fact]
    public void Build_IntersectionWithNoCatalogQuality_IsEmptyNotError()
    {
        VoicingGridResultEnvelope result = Handler.Build(Filter(thirds: ["minor"], sevenths: ["maj7"]));

        Assert.Empty(result.Cells);
    }

    // Option A: package/user-only source selection yields no cells (no automatic ⇒ empty grid).
    [Fact]
    public void Build_SourceExcludesAutomatic_ReturnsEmptyGrid()
    {
        VoicingGridResultEnvelope result = Handler.Build(Filter(sources: ["package", "user"]));

        Assert.Empty(result.Cells);
    }

    // An explicitly empty level admits nothing (distinct from a null/unconstrained level).
    [Fact]
    public void Build_ExplicitlyEmptyLevel_ReturnsEmptyGrid()
    {
        VoicingGridResultEnvelope result = Handler.Build(Filter(thirds: []));

        Assert.Empty(result.Cells);
    }

    // Cells are ordered rows-by-quality, then by family, then by shape (IN5).
    [Fact]
    public void Build_Cells_AreOrderedByQualityThenFamilyThenShape()
    {
        IReadOnlyList<VoicingGridCell> cells = Handler.Build(Filter()).Cells;

        var keys = cells.Select(c =>
        {
            AutomaticVoicingId.TryParse(c.Id, out VoicingFamily f, out Quality q, out CagedShape s);
            return ((int)q, (int)f, (int)s);
        }).ToList();

        Assert.Equal(keys.OrderBy(k => k.Item1).ThenBy(k => k.Item2).ThenBy(k => k.Item3), keys);
    }

    private static VoicingGridFilter Filter(
        int root = RootC,
        IReadOnlyList<string>? sources = null,
        IReadOnlyList<string>? families = null,
        IReadOnlyList<string>? thirds = null,
        IReadOnlyList<string>? fifths = null,
        IReadOnlyList<string>? sevenths = null) =>
        new(root, sources, families, thirds, fifths, sevenths);
}

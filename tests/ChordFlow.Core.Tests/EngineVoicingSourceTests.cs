using ChordFlow.Features.ContentCrud;
using ChordFlow.Features.Voicings;
using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

public class EngineVoicingSourceTests
{
    private readonly EngineVoicingSource _source = new();

    [Fact]
    public void List_Voicing_Yields36AutomaticRows()
    {
        IReadOnlyList<ContentItem> rows = _source.List(ContentEntity.Voicing);

        Assert.Equal(36, rows.Count);
        Assert.All(rows, r => Assert.Equal("automatic", r.Source));
        Assert.All(rows, r => Assert.Null(r.PackName));
    }

    [Fact]
    public void List_Voicing_IdsAreUniqueAndParseBack()
    {
        IReadOnlyList<ContentItem> rows = _source.List(ContentEntity.Voicing);

        Assert.Equal(rows.Count, rows.Select(r => r.Id).Distinct().Count());
        Assert.All(rows, r => Assert.True(AutomaticVoicingId.TryParse(r.Id, out _, out _), $"unparseable id {r.Id}"));
        Assert.Contains(rows, r => r.Id == "auto:dom7:E");
    }

    [Theory]
    [InlineData(ContentEntity.Progression)]
    [InlineData(ContentEntity.Song)]
    [InlineData(ContentEntity.Rhythm)]
    public void List_NonVoicingKinds_AreEmpty(ContentEntity entity)
    {
        Assert.Empty(_source.List(entity));
    }
}

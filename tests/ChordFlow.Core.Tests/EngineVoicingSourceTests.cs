using ChordFlow.Features.ContentCrud;
using ChordFlow.Features.Voicings;
using ChordFlow.Instruments.Guitar;
using Xunit;

namespace ChordFlow.Core.Tests;

public class EngineVoicingSourceTests
{
    private readonly EngineVoicingSource _source = new();

    [Fact]
    public void List_Voicing_Yields91AutomaticRows()
    {
        IReadOnlyList<ContentItem> rows = _source.List(ContentEntity.Voicing);

        // caged 46 + doubled-shell 4 + shell 14 = 64.
        Assert.Equal(64, rows.Count);
        Assert.All(rows, r => Assert.Equal("automatic", r.Source));
        Assert.All(rows, r => Assert.Null(r.PackName));
    }

    [Fact]
    public void List_Voicing_IdsAreUniqueAndParseBack()
    {
        IReadOnlyList<ContentItem> rows = _source.List(ContentEntity.Voicing);

        Assert.Equal(rows.Count, rows.Select(r => r.Id).Distinct().Count());
        Assert.All(rows, r => Assert.True(AutomaticVoicingId.TryParse(r.Id, out _, out _, out _), $"unparseable id {r.Id}"));
        Assert.Contains(rows, r => r.Id == "auto:caged:dom7:E");
        Assert.Contains(rows, r => r.Id == "auto:shell:maj7:C");
        Assert.Contains(rows, r => r.Id == "auto:dshell:dom7:C");
    }

    [Fact]
    public void DisplayNames_AreFamilyQualified()
    {
        Assert.Equal("Dominant 7 — E shape", EngineVoicingSource.DisplayNameFor("auto:caged:dom7:E"));
        Assert.Equal("Dominant 7 (shell) — E shape", EngineVoicingSource.DisplayNameFor("auto:shell:dom7:E"));
        Assert.Equal("Dominant 7 (doubled shell) — C shape", EngineVoicingSource.DisplayNameFor("auto:dshell:dom7:C"));
        Assert.Null(EngineVoicingSource.DisplayNameFor("not-an-auto-id"));
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

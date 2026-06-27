using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

public class CagedVoicingCatalogTests
{
    [Fact]
    public void Combos_Has64_AcrossThreeFamilies()
    {
        Assert.Equal(64, CagedVoicingCatalog.Combos.Count);
        Assert.Equal(46, CagedVoicingCatalog.Combos.Count(c => c.Family == VoicingFamily.Caged));
        Assert.Equal(4, CagedVoicingCatalog.Combos.Count(c => c.Family == VoicingFamily.DoubledShell));
        Assert.Equal(14, CagedVoicingCatalog.Combos.Count(c => c.Family == VoicingFamily.Shell));
    }

    [Fact]
    public void Caged_CoversFiveOrThreeShapes_IncludingTriads()
    {
        Assert.Equal(5, CagedVoicingCatalog.ShapesFor(VoicingFamily.Caged, Quality.Dominant7).Count);
        Assert.Equal(3, CagedVoicingCatalog.ShapesFor(VoicingFamily.Caged, Quality.HalfDiminished7).Count);
        Assert.Equal(5, CagedVoicingCatalog.ShapesFor(VoicingFamily.Caged, Quality.Major).Count);
    }

    [Fact]
    public void Shell_HasTwoForms_For7thOr6thOnly()
    {
        Assert.Equal(new[] { CagedShape.C, CagedShape.E }, CagedVoicingCatalog.ShapesFor(VoicingFamily.Shell, Quality.Dominant7));
        Assert.Equal(new[] { CagedShape.C, CagedShape.E }, CagedVoicingCatalog.ShapesFor(VoicingFamily.Shell, Quality.Diminished7));
        Assert.Empty(CagedVoicingCatalog.ShapesFor(VoicingFamily.Shell, Quality.Major));      // triad: no shell
        Assert.Empty(CagedVoicingCatalog.ShapesFor(VoicingFamily.Shell, Quality.Augmented));
    }

    [Fact]
    public void DoubledShell_IsCFormOnly_ForTheCuratedDoubledRootQualities()
    {
        Assert.Equal(new[] { CagedShape.C }, CagedVoicingCatalog.ShapesFor(VoicingFamily.DoubledShell, Quality.Dominant7));
        Assert.Equal(new[] { CagedShape.C }, CagedVoicingCatalog.ShapesFor(VoicingFamily.DoubledShell, Quality.Diminished7));
        Assert.Equal(new[] { CagedShape.C }, CagedVoicingCatalog.ShapesFor(VoicingFamily.DoubledShell, Quality.Major6));
        Assert.Equal(new[] { CagedShape.C }, CagedVoicingCatalog.ShapesFor(VoicingFamily.DoubledShell, Quality.Minor6));
        // Curated out: maj7/min7/m7b5 doubled-shells, and every non-C form.
        Assert.Empty(CagedVoicingCatalog.ShapesFor(VoicingFamily.DoubledShell, Quality.Major7));
        Assert.Empty(CagedVoicingCatalog.ShapesFor(VoicingFamily.DoubledShell, Quality.Minor7));
        Assert.Empty(CagedVoicingCatalog.ShapesFor(VoicingFamily.DoubledShell, Quality.HalfDiminished7));
        Assert.Empty(CagedVoicingCatalog.ShapesFor(VoicingFamily.DoubledShell, Quality.Major));
    }

    [Theory]
    [InlineData(Quality.Dominant7, true)]
    [InlineData(Quality.Major6, true)]
    [InlineData(Quality.Diminished7, true)]
    [InlineData(Quality.Major, false)]
    [InlineData(Quality.Augmented, false)]
    public void IsShellEligible(Quality quality, bool eligible)
    {
        Assert.Equal(eligible, CagedVoicingCatalog.IsShellEligible(quality));
    }
}

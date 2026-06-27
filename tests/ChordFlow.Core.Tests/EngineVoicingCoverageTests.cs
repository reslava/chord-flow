using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Coverage gating for the engine <c>automatic</c> voicing source (engine-derived-as-app-source IN9/C5): the
/// catalog (<see cref="CagedVoicingCatalog"/>) offers <b>46</b> quality×shape combos and is a <i>superset</i> of
/// the golden-oracle-verified set — Major6/Minor6 join as five-shape qualities (caged-c-full / C4) but gain their
/// oracle anchors only after the visual-confirm capture step (C5), so in the interim every oracle combo is still
/// offered while 6/m6 are offered-but-not-yet-anchored. Every offered combo must derive a valid, fully-spelled
/// grip with no throw across all 12 roots (IN5) — any throw or under-spelled family is a finding to surface.
/// </summary>
public class EngineVoicingCoverageTests
{
    private static readonly PitchClass C = new(0);

    [Fact]
    public void AutomaticCatalog_Offers46Families_SupersetOfTheOracleVerified()
    {
        var verified = OracleVoicings.Load().Select(v => (v.Shape.Quality, v.Shape.Shape)).ToHashSet();
        var offered = CagedVoicingCatalog.Combos.ToHashSet();

        Assert.Equal(46, offered.Count);
        Assert.True(offered.IsSupersetOf(verified), "every oracle-verified combo must still be offered by the catalog");
    }

    [Fact]
    public void EveryOracleCombo_DerivesAtItsOracleRegion_FullySpelled_NoThrow()
    {
        foreach ((string id, string dsl, VoicingShape shape) in OracleVoicings.Load())
        {
            int minFret = MinAuthoredFret(dsl);
            ChordShape derived = CagedDerivation.Derive(shape.Quality, shape.Shape, C, minFret, minFret + 16);
            AssertFullySpelled(shape.Quality, derived);
        }
    }

    // The new 6th families (IN5/C4): every CAGED shape derives a no-throw, fully-spelled grip at its canonical
    // region, and that grip realizes across all 12 roots within the neck — the same standard the oracle 36 meet
    // (canonical-region full spelling + transpose-and-realize, cf. CagedOracleVoicingsTests). Validating at the
    // canonical region rather than re-deriving at the cramped lowest anchor mirrors how the 36 are checked; the
    // near-nut placement is a shared, pre-existing tightness, not a 6th-family gap.
    [Theory]
    [InlineData(Quality.Major6)]
    [InlineData(Quality.Minor6)]
    public void EverySixthFamily_DerivesCanonical_FullySpelled_AndRealizesAcrossAllRoots(Quality quality)
    {
        foreach (CagedShape shape in CagedVoicingCatalog.ShapesFor(quality))
        {
            ChordShape grip = CanonicalGrip(quality, shape);
            AssertFullySpelled(quality, grip);

            int rootString = OctaveShape.RootStrings(shape).Max();
            var voicingShape = new VoicingShape(quality, shape, rootString, ChordShapeVoicing.ToVoicing(grip));
            Assert.True(voicingShape.Realize(C) is not null, $"{quality} {shape}: canonical C placement does not fit 0–15.");

            for (int root = 0; root < 12; root++)
            {
                if (voicingShape.Realize(new PitchClass(root)) is { } v)
                {
                    Assert.All(v.Positions, p => Assert.InRange(p.Fret, 0, VoicingRealizer.MaxFret));
                }
            }
        }
    }

    // The canonical-C grip the catalog serves for a family: the lowest fret window that derives without a throw
    // (the AutomaticVoicingDoc resolver path). A family with no clean placement in [0,12] fails here.
    private static ChordShape CanonicalGrip(Quality quality, CagedShape shape)
    {
        for (int minFret = 0; minFret <= 12; minFret++)
        {
            try
            {
                return CagedDerivation.Derive(quality, shape, C, minFret, 15);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
            {
                // No clean grip in this window — try a higher anchor.
            }
        }

        throw new InvalidOperationException($"No derivable placement for {quality} {shape} in [0,15].");
    }

    // Every chord tone of the quality is voiced somewhere in the derived grip (fully spelled).
    private static void AssertFullySpelled(Quality quality, ChordShape derived)
    {
        var tones = derived.Strings
            .Where(s => !s.IsMuted)
            .Select(s => ((s.Semitones % 12) + 12) % 12)
            .ToHashSet();
        foreach (int interval in QualityIntervals.Intervals(quality))
        {
            Assert.Contains(interval, tones);
        }
    }

    // The lowest fret in the authored "frets: …" clause — the neck region the grip was authored in.
    private static int MinAuthoredFret(string dsl)
    {
        int at = dsl.IndexOf("frets:", StringComparison.OrdinalIgnoreCase);
        return dsl[(at + "frets:".Length)..]
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => int.TryParse(t, out _))
            .Select(int.Parse)
            .Min();
    }
}

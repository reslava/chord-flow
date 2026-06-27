using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Coverage + structural gating for the family catalog (req IN14): every offered (family, quality, shape) has a
/// canonical grip with no throw, and each family's grip is structurally what it claims — shells are exactly
/// {root, 3rd, 7th|6th}, doubled-shells sound no fifth, and the caged dispatch matches <see cref="CagedDerivation"/>
/// (the Family=caged no-regression guard).
/// </summary>
public class VoicingCatalogCoverageTests
{
    private static readonly PitchClass C = new(0);

    [Fact]
    public void EveryOfferedCombo_HasACanonicalGrip_StructurallyValid()
    {
        foreach ((VoicingFamily family, Quality quality, CagedShape shape) in CagedVoicingCatalog.Combos)
        {
            ChordShape grip = CanonicalGrip(family, quality, shape);
            var sounded = grip.Strings.Where(s => !s.IsMuted).Select(s => Mod12(s.Semitones)).ToList();
            Assert.NotEmpty(sounded);

            int? fifth = FifthSemitone(quality);
            switch (family)
            {
                case VoicingFamily.Shell:
                    // Exactly the three guide tones: root, third, seventh|sixth — no fifth, no doublings.
                    Assert.Equal(3, sounded.Count);
                    Assert.Contains(0, sounded);
                    Assert.Contains(ThirdSemitone(quality), sounded);
                    Assert.Contains(SeventhOrSixthSemitone(quality), sounded);
                    Assert.DoesNotContain(fifth!.Value, sounded);
                    break;
                case VoicingFamily.DoubledShell:
                    // The 5th is muted; the guide tones survive.
                    Assert.DoesNotContain(fifth!.Value, sounded);
                    Assert.Contains(0, sounded);
                    break;
                case VoicingFamily.Caged:
                    // Fully spelled: every chord tone is voiced somewhere.
                    foreach (int interval in QualityIntervals.Intervals(quality))
                    {
                        Assert.Contains(interval, sounded);
                    }
                    break;
            }
        }
    }

    [Fact]
    public void CagedDispatch_MatchesCagedDerivation_NoRegression()
    {
        foreach ((Quality quality, CagedShape shape) in CagedVoicingCatalog.Combos
                     .Where(c => c.Family == VoicingFamily.Caged).Select(c => (c.Quality, c.Shape)))
        {
            for (int root = 0; root < 12; root++)
            {
                var pc = new PitchClass(root);
                ChordShape direct, viaFamily;
                try { direct = CagedDerivation.Derive(quality, shape, pc, 0, 15); }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException) { continue; }

                viaFamily = FamilyVoicing.Derive(VoicingFamily.Caged, quality, shape, pc, 0, 15);
                Assert.Equal(direct.FretString(), viaFamily.FretString());
            }
        }
    }

    // The lowest fret window that derives without a throw — the resolver's region filter for one shape.
    private static ChordShape CanonicalGrip(VoicingFamily family, Quality quality, CagedShape shape)
    {
        for (int minFret = 0; minFret <= 12; minFret++)
        {
            try { return FamilyVoicing.Derive(family, quality, shape, C, minFret, 15); }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException) { }
        }

        throw new InvalidOperationException($"No derivable {family} placement for {quality} {shape} in [0,15].");
    }

    private static int? FifthSemitone(Quality quality) =>
        ChordTones.Of(new Chord(C, quality)).Where(t => t.Function == ChordToneFunction.Fifth)
            .Select(t => (int?)Mod12(t.Interval)).FirstOrDefault();

    private static int ThirdSemitone(Quality quality) =>
        Mod12(ChordTones.Of(new Chord(C, quality)).First(t => t.Function == ChordToneFunction.Third).Interval);

    private static int SeventhOrSixthSemitone(Quality quality) =>
        Mod12(ChordTones.Of(new Chord(C, quality))
            .First(t => t.Function is ChordToneFunction.Seventh or ChordToneFunction.Sixth).Interval);

    private static int Mod12(int v) => ((v % 12) + 12) % 12;
}

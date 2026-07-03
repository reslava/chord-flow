using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The trace contract of the reified voicings engine (voicings-engine, req IN14): every operator emits a
/// <see cref="VoicingDerivation"/> whose abstract <see cref="ToneSelection"/> matches the quality formula by
/// function per family, and whose <see cref="RealizationStep"/>s are consistent with the realized grip. (Grip
/// byte-identity itself is proven by the pre-existing oracle/coverage suites.)
/// </summary>
public class VoicingDerivationTests
{
    private static readonly PitchClass C = new(0);

    [Fact]
    public void Caged_ToneSelection_IsEveryChordToneFunction()
    {
        VoicingDerivation d = FamilyVoicing.Voicing(VoicingFamily.Caged, Quality.Dominant7, CagedShape.E, C, 0, 15);

        var got = d.ToneSelection.Select(t => t.Function).ToHashSet();
        var expected = ChordTones.Of(new Chord(C, Quality.Dominant7)).Select(t => t.Function).ToHashSet();

        Assert.Equal(expected, got);
        Assert.Contains(ChordToneFunction.Fifth, got); // CAGED keeps the 5th
    }

    [Fact]
    public void Shell_And_DoubledShell_ToneSelection_OmitTheFifth_KeepRootThirdGuide()
    {
        foreach (VoicingFamily family in new[] { VoicingFamily.Shell, VoicingFamily.DoubledShell })
        {
            CagedShape shape = family == VoicingFamily.Shell ? CagedShape.E : CagedShape.C;
            VoicingDerivation d = FamilyVoicing.Voicing(family, Quality.Dominant7, shape, C, 0, 15);

            var funcs = d.ToneSelection.Select(t => t.Function).ToHashSet();
            Assert.DoesNotContain(ChordToneFunction.Fifth, funcs);
            Assert.Contains(ChordToneFunction.Root, funcs);
            Assert.Contains(ChordToneFunction.Third, funcs);
            Assert.Contains(ChordToneFunction.Seventh, funcs);
        }
    }

    [Fact]
    public void EveryCombo_ToneSelection_MatchesItsFamilyRule()
    {
        foreach ((VoicingFamily family, Quality quality, CagedShape shape) in CagedVoicingCatalog.Combos)
        {
            VoicingDerivation d;
            try
            {
                d = FamilyVoicing.Voicing(family, quality, shape, C, 0, 15);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
            {
                continue; // no clean grip here — not a trace concern
            }

            var got = d.ToneSelection.Select(t => t.Function).OrderBy(f => f).ToList();
            IEnumerable<ChordToneFunction> all = ChordTones.Of(new Chord(C, quality)).Select(t => t.Function);
            var expected = (family == VoicingFamily.Caged ? all : all.Where(f => f != ChordToneFunction.Fifth))
                .OrderBy(f => f)
                .ToList();

            Assert.Equal(expected, got);
        }
    }

    [Fact]
    public void EveryCombo_AllRoots_Realization_IsConsistentWithTheGrip()
    {
        foreach ((VoicingFamily family, Quality quality, CagedShape shape) in CagedVoicingCatalog.Combos)
        {
            for (int root = 0; root < 12; root++)
            {
                VoicingDerivation d;
                try
                {
                    d = FamilyVoicing.Voicing(family, quality, shape, new PitchClass(root), 0, 15);
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
                {
                    continue;
                }

                Assert.Equal(family, d.Family);
                Assert.NotEmpty(d.Realization);
                Assert.Contains(d.Realization, s => s.Kind == RealizationStepKind.AnchorFinger);
                Assert.Contains(d.Grip.Strings, s => !s.IsMuted);
            }
        }
    }

    [Fact]
    public void Caged_SelectAndMuteSteps_PartitionTheGripStrings()
    {
        VoicingDerivation d = FamilyVoicing.Voicing(VoicingFamily.Caged, Quality.Major, CagedShape.E, C, 0, 15);

        var sounded = d.Grip.Strings.Where(s => !s.IsMuted).Select(s => s.String).ToHashSet();
        var muted = d.Grip.Strings.Where(s => s.IsMuted).Select(s => s.String).ToHashSet();

        RealizationStep select = d.Realization.Single(s => s.Kind == RealizationStepKind.Select);
        RealizationStep mute = d.Realization.Last(s => s.Kind == RealizationStepKind.Mute);

        Assert.Equal(sounded, select.Strings!.ToHashSet());
        Assert.Equal(muted, mute.Strings!.ToHashSet());
    }

    [Fact]
    public void DoubledShell_ReduceStepIsLast_AndMutesOnlyGripMutedStrings()
    {
        VoicingDerivation d = FamilyVoicing.Voicing(VoicingFamily.DoubledShell, Quality.Dominant7, CagedShape.C, C, 0, 15);

        RealizationStep reduce = d.Realization[^1];
        Assert.Equal(RealizationStepKind.Reduce, reduce.Kind);
        Assert.Equal(OperatorKind.Reduce, d.Kind);

        foreach (int s in reduce.Strings!)
        {
            Assert.True(d.Grip.Strings.First(x => x.String == s).IsMuted);
        }
    }
}

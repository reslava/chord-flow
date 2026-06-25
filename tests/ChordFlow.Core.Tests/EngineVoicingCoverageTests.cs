using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Coverage gating for the engine <c>automatic</c> voicing source (engine-derived-as-app-source IN9/C5): the
/// set of quality×shape combos the source offers (<see cref="CagedVoicingCatalog"/>) is <b>exactly</b> the 36
/// the golden oracle verifies — no derived-but-unverified combo (e.g. an m7♭5 C-shape) is ever offered — and
/// every offered combo derives a valid, fully-spelled grip with no throw at its oracle region.
/// </summary>
public class EngineVoicingCoverageTests
{
    private static readonly PitchClass C = new(0);

    [Fact]
    public void AutomaticCatalog_OffersExactlyTheOracleVerified36()
    {
        var verified = OracleVoicings.Load().Select(v => (v.Shape.Quality, v.Shape.Shape)).ToHashSet();
        var offered = CagedVoicingCatalog.Combos.ToHashSet();

        Assert.Equal(36, offered.Count);
        Assert.Equal(verified, offered);
    }

    [Fact]
    public void EveryOfferedCombo_DerivesAtItsOracleRegion_FullySpelled_NoThrow()
    {
        foreach ((string id, string dsl, VoicingShape shape) in OracleVoicings.Load())
        {
            int minFret = MinAuthoredFret(dsl);
            ChordShape derived = CagedDerivation.Derive(shape.Quality, shape.Shape, C, minFret, minFret + 16);

            var tones = derived.Strings
                .Where(s => !s.IsMuted)
                .Select(s => ((s.Semitones % 12) + 12) % 12)
                .ToHashSet();
            foreach (int interval in QualityIntervals.Intervals(shape.Quality))
            {
                Assert.Contains(interval, tones); // every chord tone is voiced (fully spelled)
            }
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

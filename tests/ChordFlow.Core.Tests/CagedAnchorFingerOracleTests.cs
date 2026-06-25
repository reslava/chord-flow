using ChordFlow.Music.Harmony;
using System.Text;

using ChordFlow.Instruments.Guitar;
using Xunit;
using Xunit.Abstractions;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The <b>anchor-finger golden oracle</b> (req <c>IN7</c>): every authored voicing carries one <c>anchor:</c> token
/// (its real fingering anchor), and the engine's derived <see cref="ChordShape.AnchorFinger"/> must match it. This
/// falsifies the anchor-finger rule directly — the frets oracle only proves it indirectly. Each voicing is derived in
/// the region of its authored frets (the canonical form folds high grips to the nut, which would add open strings and
/// change the fretted width).
/// </summary>
public class CagedAnchorFingerOracleTests
{
    private static readonly PitchClass C = new(0);
    private readonly ITestOutputHelper _out;

    public CagedAnchorFingerOracleTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void DerivedAnchor_MatchesAuthoredAnchor_ForEveryAnnotatedVoicing()
    {
        var voicings = OracleVoicings.Load()
            .Where(x => x.Shape.Anchor is not null)
            .ToList();

        Assert.NotEmpty(voicings);

        var report = new StringBuilder();
        report.AppendLine($"{"id",-16} {"authored",-8} {"derived",-8} match");
        report.AppendLine(new string('-', 44));

        int match = 0;
        foreach ((string id, string dsl, VoicingShape shape) in voicings)
        {
            int minFret = MinAuthoredFret(dsl);
            ChordShape derived = CagedDerivation.Derive(shape.Quality, shape.Shape, C, minFret, minFret + 16);

            bool ok = derived.AnchorFinger == shape.Anchor!.Value;
            if (ok) match++;
            report.AppendLine($"{id,-16} {shape.Anchor,-8} {derived.AnchorFinger,-8} {(ok ? "OK" : "DIFF")}");
        }

        report.AppendLine(new string('-', 44));
        report.AppendLine($"matched {match}/{voicings.Count}");
        _out.WriteLine(report.ToString());

        Assert.True(match == voicings.Count, "\n" + report);
    }

    // The lowest fret in the authored "frets: …" clause (open 0 included) — the neck region the grip was authored in.
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

using ChordFlow.Music.Harmony;
using System.Text;

using ChordFlow.Instruments.Guitar;
using Xunit;
using Xunit.Abstractions;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Golden-oracle comparison: derive every authored (quality, shape) at C and print authored vs derived frets
/// side by side. This is the calibration harness for the CAGED engine — the authored grips are the spec.
/// </summary>
public class CagedDerivationOracleTests
{
    private static readonly PitchClass C = new(0);
    private readonly ITestOutputHelper _out;

    public CagedDerivationOracleTests(ITestOutputHelper output) => _out = output;

    // The 34 authored voicings (Content/default-pack/voicings/*.dsl), frets low-E→high-E (string 6 → 1).
    private static readonly (Quality Quality, CagedShape Shape, string Frets)[] Authored =
    {
        (Quality.Major, CagedShape.C, "x 3 2 0 1 0"),
        (Quality.Major, CagedShape.A, "x 3 5 5 5 3"),
        (Quality.Major, CagedShape.G, "8 7 5 5 5 8"),
        (Quality.Major, CagedShape.E, "8 10 10 9 8 8"),
        (Quality.Major, CagedShape.D, "x x 10 12 13 12"),

        (Quality.Minor, CagedShape.C, "x 15 13 12 13 15"),
        (Quality.Minor, CagedShape.A, "x 3 5 5 4 3"),
        (Quality.Minor, CagedShape.G, "8 6 5 5 8 8"),
        (Quality.Minor, CagedShape.E, "8 10 10 8 8 8"),
        (Quality.Minor, CagedShape.D, "x x 10 12 13 11"),

        (Quality.Major7, CagedShape.C, "x 3 2 0 0 0"),
        (Quality.Major7, CagedShape.A, "x 3 5 4 5 3"),
        (Quality.Major7, CagedShape.G, "8 7 5 5 5 7"),
        (Quality.Major7, CagedShape.E, "8 10 9 9 8 8"),
        (Quality.Major7, CagedShape.D, "x x 10 12 12 12"),

        (Quality.Dominant7, CagedShape.C, "x 3 2 3 1 3"),
        (Quality.Dominant7, CagedShape.A, "x 3 5 3 5 3"),
        (Quality.Dominant7, CagedShape.G, "8 7 8 5 8 8"),
        (Quality.Dominant7, CagedShape.E, "8 10 8 9 8 8"),
        (Quality.Dominant7, CagedShape.D, "x x 10 12 11 12"),

        (Quality.Minor7, CagedShape.C, "x 15 13 15 13 15"),
        (Quality.Minor7, CagedShape.A, "x 3 5 3 4 3"),
        (Quality.Minor7, CagedShape.G, "8 6 8 5 8 8"),
        (Quality.Minor7, CagedShape.E, "8 10 8 8 8 8"),
        (Quality.Minor7, CagedShape.D, "x x 10 12 11 11"),

        (Quality.HalfDiminished7, CagedShape.A, "x 3 4 3 4 6"),
        (Quality.HalfDiminished7, CagedShape.E, "8 9 8 8 11 8"),
        (Quality.HalfDiminished7, CagedShape.D, "x x 10 11 11 11"),

        (Quality.Diminished7, CagedShape.A, "x 3 4 2 4 5"),
        (Quality.Diminished7, CagedShape.E, "8 9 7 8 10 8"),
        (Quality.Diminished7, CagedShape.D, "x x 10 11 10 11"),

        (Quality.Augmented, CagedShape.C, "x 3 2 1 1 0"),
        (Quality.Augmented, CagedShape.A, "x 3 6 5 5 4"),
        (Quality.Augmented, CagedShape.G, "8 7 6 5 5 8"),
        (Quality.Augmented, CagedShape.E, "8 11 10 9 9 8"),
        (Quality.Augmented, CagedShape.D, "x x 10 13 13 12"),
    };

    [Fact]
    public void Derived_VsAuthored_SideBySide()
    {
        var report = new StringBuilder();
        report.AppendLine($"{"quality",-16} {"shape",-5} | {"authored",-18} | {"derived",-18} | match");
        report.AppendLine(new string('-', 72));

        int match = 0;
        foreach ((Quality quality, CagedShape shape, string authored) in Authored)
        {
            int minFret = authored.Split(' ')
                .Where(t => int.TryParse(t, out _))
                .Select(int.Parse)
                .Min();

            string derived;
            try
            {
                derived = CagedDerivation.Derive(quality, shape, C, minFret, minFret + 16).FretString();
            }
            catch (Exception ex)
            {
                derived = $"<{ex.GetType().Name}>";
                _out.WriteLine(ex.ToString());
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(System.AppContext.BaseDirectory, "caged-oracle-error.txt"), ex.ToString());
            }

            bool ok = derived == authored;
            if (ok) match++;
            report.AppendLine($"{quality,-16} {shape,-5} | {authored,-18} | {derived,-18} | {(ok ? "OK" : "DIFF")}");
        }

        report.AppendLine(new string('-', 72));
        report.AppendLine($"matched {match}/{Authored.Length}");
        _out.WriteLine(report.ToString());
        System.IO.File.WriteAllText(
            System.IO.Path.Combine(System.AppContext.BaseDirectory, "caged-oracle-report.txt"), report.ToString());

        Assert.True(match == Authored.Length, "\n" + report);
    }
}

using System.Text;
using ChordFlow.Features.Packs;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Rhythm;
using ChordFlow.Persistence;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The catalog-driven golden oracle for <see cref="HarmonicAnalyzer"/> (IN12). Every seeded default-pack
/// progression is realized into concrete chords — the major-frame set in C major, the minor-home set in
/// A minor (the <see cref="ProgressionSeedTests.MinorProgression_RealizesToExpectedChordsInAMinor"/> pin) —
/// and the analyzer's <c>(Category, Target, SourceMode)</c> per chord is asserted against the human-reasoned
/// oracle authored in <c>loom/refs/harmonic-analysis-oracle-reference.md</c> (IN11). The reference doc is the
/// single source of the expectations; this test parses it. A completeness guard fails if a seeded progression
/// has no oracle section (or vice-versa), so a new progression cannot silently escape analysis.
///
/// <para>Covers both major and minor tonic natively (IN8): the harmonic-minor V/leading-tone, Picardy, and
/// borrowing — and the dominant-blues must-not-over-label case (IN10: I7/IV7 = Chromatic, V7 = Diatonic).
/// The realize adapter is test-side (C2) — no Music-layer dependency on Song/Realized types.</para>
/// </summary>
public class HarmonicAnalyzerCatalogTests
{
    // ---- realize adapter (test-side, C2) -----------------------------------

    private static Key PinnedKey(Tonality tonality) => tonality == Tonality.Minor
        ? new Key(new PitchClass(9), IsMinor: true)  // A minor
        : new Key(new PitchClass(0), IsMinor: false); // C major

    private static (Key Key, IReadOnlyList<Chord> Chords) Realize(PackDefinition def)
    {
        (CatalogMetadata meta, string body) = CatalogHeader.Parse(def.Dsl);
        Progression prog = ProgressionParser.Parse(def.Id, def.Name, body, TimeSignature.FourFour, home: meta.Tonality);
        Key key = PinnedKey(meta.Tonality);

        // RealizeBars (not the one-chord-per-bar Realize) so multi-chord bars like `17_67` contribute every
        // chord, matching the oracle's flat per-chord sequence.
        IReadOnlyList<Chord> chords = Transposer.RealizeBars(prog, key)
            .SelectMany(bar => bar.Spans)
            .Select(span => span.Chord)
            .ToList();

        return (key, chords);
    }

    private static PackDefinition Progression(string id) =>
        DefaultPack.Load().Definitions.Single(d => d.Kind == ContentKind.Progression && d.Id == id);

    public static IEnumerable<object[]> ProgressionIds() =>
        DefaultPack.Load().Definitions
            .Where(d => d.Kind == ContentKind.Progression)
            .Select(d => new object[] { d.Id });

    // ---- the golden assertion ----------------------------------------------

    [Theory]
    [MemberData(nameof(ProgressionIds))]
    public void SeededProgression_AnalyzesToTheOracle(string id)
    {
        PackDefinition def = Progression(id);
        (Key key, IReadOnlyList<Chord> chords) = Realize(def);

        Assert.True(Oracle.TryGetValue(id, out IReadOnlyList<ExpectedRow>? expected),
            $"No oracle section for '{id}' in harmonic-analysis-oracle-reference.md.");

        Assert.Equal(expected!.Count, chords.Count); // the oracle must have one row per realized chord

        for (int i = 0; i < chords.Count; i++)
        {
            ChordAnalysis actual = HarmonicAnalyzer.Analyze(chords[i], key);
            ExpectedRow row = expected[i];
            string where = $"{id} chord #{i + 1} ({ChordSymbol.Format(chords[i], key)})";

            Assert.True(row.Category == actual.Category, $"{where}: expected Category {row.Category}, got {actual.Category}");
            Assert.True(row.Target == actual.Target?.Number, $"{where}: expected Target {row.Target?.ToString() ?? "—"}, got {actual.Target?.Number.ToString() ?? "—"}");
            Assert.True(row.SourceMode == actual.SourceMode, $"{where}: expected SourceMode {row.SourceMode?.ToString() ?? "—"}, got {actual.SourceMode?.ToString() ?? "—"}");
        }
    }

    [Fact]
    public void OracleAndCatalog_AreInLockstep_NoOrphansEitherWay()
    {
        var catalogIds = DefaultPack.Load().Definitions
            .Where(d => d.Kind == ContentKind.Progression)
            .Select(d => d.Id)
            .ToHashSet();

        foreach (string id in catalogIds)
        {
            Assert.True(Oracle.ContainsKey(id), $"Seeded progression '{id}' has no oracle section — add it to harmonic-analysis-oracle-reference.md.");
        }

        foreach (string id in Oracle.Keys)
        {
            Assert.True(catalogIds.Contains(id), $"Oracle section '{id}' has no matching seeded progression — a stale oracle entry.");
        }
    }

    // ---- engine-output dump for human review (Rafa: append actual results) --

    [Fact]
    public void EmitActualEngineOutput_ForReview()
    {
        var byTonality = DefaultPack.Load().Definitions
            .Where(d => d.Kind == ContentKind.Progression)
            .Select(d => (Def: d, Meta: CatalogHeader.Parse(d.Dsl).Item1))
            .OrderBy(x => x.Meta.Tonality) // Major (0) then Minor (1)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("<!-- generated by HarmonicAnalyzerCatalogTests.EmitActualEngineOutput_ForReview -->");
        foreach ((PackDefinition def, CatalogMetadata meta) in byTonality)
        {
            (Key key, IReadOnlyList<Chord> chords) = Realize(def);
            string home = meta.Tonality == Tonality.Minor ? "A minor" : "C major";
            sb.AppendLine();
            // No backticks around the id: the oracle parser keys on a backticked id, so these engine-dump
            // sections stay inert when appended into the reference doc (they must not shadow the expected tables).
            sb.AppendLine($"### {def.Id} — engine ({home})");
            sb.AppendLine();
            sb.AppendLine("| # | Chord | Category | Target | SourceMode |");
            sb.AppendLine("|---|-------|----------|--------|------------|");
            for (int i = 0; i < chords.Count; i++)
            {
                ChordAnalysis a = HarmonicAnalyzer.Analyze(chords[i], key);
                string target = a.Target?.Number.ToString() ?? "—";
                string src = a.SourceMode?.ToString() ?? "—";
                sb.AppendLine($"| {i + 1} | {ChordSymbol.Format(chords[i], key)} | {a.Category} | {target} | {src} |");
            }
        }

        string outPath = Path.Combine(AppContext.BaseDirectory, "harmonic-analysis-oracle.actual.md");
        File.WriteAllText(outPath, sb.ToString());
        Assert.True(File.Exists(outPath));
    }

    // ---- oracle-doc parser (the ref doc is the single source of expectations) --

    private sealed record ExpectedRow(HarmonicCategory Category, int? Target, KeyMode? SourceMode);

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<ExpectedRow>>> OracleCache =
        new(() => ParseOracle(OracleDocPath()));

    private static IReadOnlyDictionary<string, IReadOnlyList<ExpectedRow>> Oracle => OracleCache.Value;

    private static string OracleDocPath()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "loom", "refs", "harmonic-analysis-oracle-reference.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"Could not locate loom/refs/harmonic-analysis-oracle-reference.md by walking up from {AppContext.BaseDirectory}.");
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<ExpectedRow>> ParseOracle(string path)
    {
        var result = new Dictionary<string, IReadOnlyList<ExpectedRow>>();
        string? currentId = null;
        List<ExpectedRow>? rows = null;
        bool headerSeen = false;
        int catCol = -1, targetCol = -1, srcCol = -1;

        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.Trim();

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                // Any heading ends the current section; only a `### `-with-backtick heading starts a new oracle
                // section. This makes the appended `#### … — engine` dump (and `##` headings) inert.
                currentId = line.StartsWith("### ", StringComparison.Ordinal) ? BacktickedToken(line) : null;
                rows = null;
                headerSeen = false;
                continue;
            }

            if (currentId is null || rows is not null && !line.StartsWith("|", StringComparison.Ordinal))
            {
                // Outside a section, or the (single) table for this section has ended.
                continue;
            }

            if (!line.StartsWith("|", StringComparison.Ordinal))
            {
                continue;
            }

            string[] cells = SplitRow(line);

            if (!headerSeen)
            {
                int c = Array.FindIndex(cells, s => s.Equals("Category", StringComparison.OrdinalIgnoreCase));
                if (c < 0)
                {
                    continue; // not the oracle table header
                }

                catCol = c;
                targetCol = Array.FindIndex(cells, s => s.Equals("Target", StringComparison.OrdinalIgnoreCase));
                srcCol = Array.FindIndex(cells, s => s.Equals("SourceMode", StringComparison.OrdinalIgnoreCase));
                rows = new List<ExpectedRow>();
                result[currentId!] = rows;
                headerSeen = true;
                continue;
            }

            if (IsSeparator(cells))
            {
                continue;
            }

            rows!.Add(new ExpectedRow(
                Enum.Parse<HarmonicCategory>(cells[catCol], ignoreCase: true),
                NullableInt(cells[targetCol]),
                NullableMode(cells[srcCol])));
        }

        return result;
    }

    private static string? BacktickedToken(string line)
    {
        int a = line.IndexOf('`');
        int b = a >= 0 ? line.IndexOf('`', a + 1) : -1;
        return a >= 0 && b > a ? line[(a + 1)..b] : null;
    }

    private static string[] SplitRow(string line) =>
        line.Trim('|').Split('|').Select(s => s.Trim()).ToArray();

    private static bool IsSeparator(IEnumerable<string> cells) =>
        cells.All(c => c.Length > 0 && c.All(ch => ch is '-' or ':'));

    private static int? NullableInt(string cell) => int.TryParse(cell, out int v) ? v : null;

    private static KeyMode? NullableMode(string cell) => cell.Trim() switch
    {
        "Major" => KeyMode.Major,
        "Minor" => KeyMode.Minor,
        _ => null,
    };
}

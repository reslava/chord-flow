using ChordFlow.Features.Packs;
using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Core.Tests;

/// <summary>
/// Loads the <b>golden-oracle voicing fixture</b> — the 36 authored CAGED grips that the app no longer ships
/// (engine-derived-as-app-source IN8). They live under <c>fixtures/caged-oracle/*.dsl</c> (copied next to the
/// test assembly) and exist only to verify the engine (<see cref="CagedDerivation"/>) against the authored
/// fingerings. Each <c>.dsl</c> is a plain voicing line (no catalog header), so <see cref="VoicingDslParser"/>
/// reads it directly — the same parser the pack used.
/// </summary>
internal static class OracleVoicings
{
    private static string FixtureDir => Path.Combine(AppContext.BaseDirectory, "fixtures", "caged-oracle");

    /// <summary>The fixture grips: file-stem id, the voicing DSL line (name header peeled), and the parsed shape.</summary>
    public static IReadOnlyList<(string Id, string Dsl, VoicingShape Shape)> Load() =>
        Directory.EnumerateFiles(FixtureDir, "*.dsl")
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f =>
            {
                // Read the same way the pack did — peel the optional `name:` header, trim — so the parser sees
                // exactly the voicing line.
                PackDefinition def = PackDefinitionFile.Read(ContentKind.Voicing, Path.GetFileName(f), File.ReadAllText(f));
                return (def.Id, def.Dsl, Shape: VoicingDslParser.Parse(def.Dsl));
            })
            .ToList();
}

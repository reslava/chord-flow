using ChordFlow.Music.Harmony;
using System.Linq;
using NetArchTest.Rules;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The compile-time-adjacent guard for the theory ↔ instrument boundary (guitar/instrument-boundary).
/// Pure music theory under <c>ChordFlow.Music</c> must never depend on the guitar adapter under
/// <c>ChordFlow.Instruments</c>, so the kernel stays provably instrument-agnostic and guitar is an
/// opt-in adapter. NetArchTest does IL-level dependency analysis, so a method-body reference (not just
/// public surface) trips it.
/// </summary>
/// <remarks>
/// Scope is the <b>Music edge only</b>, by design: <c>Rendering → Instruments</c> and
/// <c>Persistence → Instruments</c> are legitimate (the tab renderer and voicing store consume guitar
/// fret positions) and are intentionally NOT constrained here.
/// </remarks>
public class InstrumentBoundaryTests
{
    private static readonly System.Reflection.Assembly Core = typeof(PitchClass).Assembly;

    [Fact]
    public void Music_DoesNotDependOn_Instruments()
    {
        TestResult result = Types.InAssembly(Core)
            .That().ResideInNamespaceStartingWith("ChordFlow.Music")
            .ShouldNot().HaveDependencyOn("ChordFlow.Instruments")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Music types must not reference ChordFlow.Instruments. Offending types: " +
            string.Join(", ", result.FailingTypeNames ?? Enumerable.Empty<string>()));
    }

    // Guard against a vacuous pass: if the namespace filter ever matched nothing (a typo, a rename),
    // the rule above would pass for the wrong reason. Assert the Music type set is actually populated.
    [Fact]
    public void Music_NamespaceFilter_MatchesTypes()
    {
        int musicTypes = Types.InAssembly(Core)
            .That().ResideInNamespaceStartingWith("ChordFlow.Music")
            .GetTypes().Count();

        Assert.True(musicTypes > 0, "Expected ChordFlow.Music to contain types; the boundary rule would otherwise pass vacuously.");
    }
}

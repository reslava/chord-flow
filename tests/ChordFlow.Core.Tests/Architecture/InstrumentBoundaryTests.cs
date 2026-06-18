using System.Linq;
using ChordFlow.Domain;
using NetArchTest.Rules;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The compile-time-adjacent guard for the theory ↔ instrument boundary (guitar/instrument-boundary).
/// Pure music theory under <c>ChordFlow.Domain</c> must never depend on the guitar adapter under
/// <c>ChordFlow.Instruments</c>, so the kernel stays provably instrument-agnostic and guitar is an
/// opt-in adapter. NetArchTest does IL-level dependency analysis, so a method-body reference (not just
/// public surface) trips it.
/// </summary>
/// <remarks>
/// Scope is the <b>Domain edge only</b>, by design: <c>Rendering → Instruments</c> and
/// <c>Persistence → Instruments</c> are legitimate (the tab renderer and voicing store consume guitar
/// fret positions) and are intentionally NOT constrained here.
/// </remarks>
public class InstrumentBoundaryTests
{
    private static readonly System.Reflection.Assembly Core = typeof(PitchClass).Assembly;

    [Fact]
    public void Domain_DoesNotDependOn_Instruments()
    {
        TestResult result = Types.InAssembly(Core)
            .That().ResideInNamespace("ChordFlow.Domain")
            .ShouldNot().HaveDependencyOn("ChordFlow.Instruments")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "Domain types must not reference ChordFlow.Instruments. Offending types: " +
            string.Join(", ", result.FailingTypeNames ?? Enumerable.Empty<string>()));
    }

    // Guard against a vacuous pass: if the namespace filter ever matched nothing (a typo, a rename),
    // the rule above would pass for the wrong reason. Assert the Domain type set is actually populated.
    [Fact]
    public void Domain_NamespaceFilter_MatchesTypes()
    {
        int domainTypes = Types.InAssembly(Core)
            .That().ResideInNamespace("ChordFlow.Domain")
            .GetTypes().Count();

        Assert.True(domainTypes > 0, "Expected ChordFlow.Domain to contain types; the boundary rule would otherwise pass vacuously.");
    }
}

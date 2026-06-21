using ChordFlow.Music.Harmony;
using System.Linq;
using NetArchTest.Rules;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// IL-level guard for the <c>ChordFlow.Music.*</c> layering (domain-to-music-rename, IN9). The kernel was
/// split from one <c>Domain</c> namespace into concept-named flat siblings; these rules freeze the
/// <b>observed</b> dependency edges so the boundaries stay a real, compiler-checked DAG rather than a
/// convention that rots. The forbidden-edge sets below collectively guarantee acyclicity.
/// </summary>
/// <remarks>
/// Observed DAG (each depends only on the ones to its right):
/// <code>
///   Exercises → Songs → Progressions → Harmony
///                    ↘  ↘ Rhythm   (Rhythm is independent; Melody → Harmony)
/// </code>
/// Sinks: <c>Harmony</c> and <c>Rhythm</c> depend on no sibling. The allow-list is the *measured* edge set
/// (Transposer was moved to Progressions to keep Harmony a true sink), not an aspirational one — so the
/// rules confirm the structure without forcing any code change.
/// </remarks>
public class MusicLayeringTests
{
    private static readonly System.Reflection.Assembly Core = typeof(PitchClass).Assembly;

    private const string Harmony = "ChordFlow.Music.Harmony";
    private const string Rhythm = "ChordFlow.Music.Rhythm";
    private const string Melody = "ChordFlow.Music.Melody";
    private const string Progressions = "ChordFlow.Music.Progressions";
    private const string Songs = "ChordFlow.Music.Songs";
    private const string Exercises = "ChordFlow.Exercises";

    [Theory]
    // namespace, the namespaces it must NOT depend on (everything outside its measured allow-list)
    [InlineData(Harmony, new[] { Rhythm, Melody, Progressions, Songs, Exercises })]   // sink
    [InlineData(Rhythm, new[] { Harmony, Melody, Progressions, Songs, Exercises })]   // sink (independent)
    [InlineData(Melody, new[] { Rhythm, Progressions, Songs, Exercises })]            // → Harmony only
    [InlineData(Progressions, new[] { Melody, Songs, Exercises })]                    // → Harmony, Rhythm
    [InlineData(Songs, new[] { Melody, Exercises })]                                  // → Harmony, Progressions, Rhythm
    public void Music_Namespace_RespectsLayering(string ns, string[] forbidden)
    {
        TestResult result = Types.InAssembly(Core)
            .That().ResideInNamespaceStartingWith(ns)
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"{ns} must not depend on [{string.Join(", ", forbidden)}]. Offending types: " +
            string.Join(", ", result.FailingTypeNames ?? Enumerable.Empty<string>()));
    }

    // Vacuous-pass guard: every layered namespace must actually contain types, else the rules above
    // pass for the wrong reason.
    [Theory]
    [InlineData(Harmony)]
    [InlineData(Rhythm)]
    [InlineData(Melody)]
    [InlineData(Progressions)]
    [InlineData(Songs)]
    public void Music_Namespace_IsPopulated(string ns)
    {
        int count = Types.InAssembly(Core)
            .That().ResideInNamespaceStartingWith(ns)
            .GetTypes().Count();

        Assert.True(count > 0, $"Expected {ns} to contain types; layering rules would otherwise pass vacuously.");
    }
}

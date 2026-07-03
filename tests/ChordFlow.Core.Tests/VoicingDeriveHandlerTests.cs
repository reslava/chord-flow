using ChordFlow.Bridge;
using ChordFlow.Features.Voicings;
using Xunit;

namespace ChordFlow.Core.Tests;

/// <summary>
/// The Voicings Engine inspector slice (voicings-engine, req IN11/IN14/IN16): <c>voicingDerive</c> returns a
/// well-formed derivation (id + abstract voicing + steps + grip diagram) and fails loud on bad input;
/// <c>voicingOperators</c> projects the registry + declared schemas so the page is schema-driven.
/// </summary>
public class VoicingDeriveHandlerTests
{
    private static readonly VoicingDeriveHandler Handler = new();

    [Fact]
    public void Derive_Caged_ReturnsWellFormedDerivation()
    {
        VoicingDerivationEnvelope env = Handler.Derive(new VoicingDeriveRequest("caged", "Dominant7", 0, "E", null, null));

        Assert.Equal("auto:caged:dom7:E", env.Id);
        Assert.Equal("caged", env.Family);
        Assert.Equal("DeriveFromFormula", env.Kind);
        Assert.Contains(env.ToneSelection, t => t.Function == "Fifth");   // CAGED keeps the 5th
        Assert.NotEmpty(env.RealizationSteps);
        Assert.NotNull(env.Diagram);
        Assert.Equal("voicingDerivation", env.Type);
    }

    [Fact]
    public void Derive_Shell_OmitsTheFifth()
    {
        VoicingDerivationEnvelope env = Handler.Derive(new VoicingDeriveRequest("shell", "Dominant7", 0, "E", null, null));

        Assert.Equal("auto:shell:dom7:E", env.Id);
        Assert.DoesNotContain(env.ToneSelection, t => t.Function == "Fifth");
        Assert.Contains(env.ToneSelection, t => t.Function == "Seventh");
    }

    [Fact]
    public void Derive_UnknownFamilyOrQualityOrShape_FailsLoud()
    {
        Assert.Throws<ArgumentException>(() => Handler.Derive(new VoicingDeriveRequest("nope", "Dominant7", 0, "E", null, null)));
        Assert.Throws<ArgumentException>(() => Handler.Derive(new VoicingDeriveRequest("caged", "Bogus", 0, "E", null, null)));
        Assert.Throws<ArgumentException>(() => Handler.Derive(new VoicingDeriveRequest("caged", "Dominant7", 0, "Z", null, null)));
    }

    [Fact]
    public void Derive_IneligibleCombo_ShellOfATriad_FailsLoud()
    {
        Assert.Throws<InvalidOperationException>(
            () => Handler.Derive(new VoicingDeriveRequest("shell", "Major", 0, "E", null, null)));
    }

    [Fact]
    public void Operators_ProjectsAllThreeWithSchemas()
    {
        VoicingOperatorsEnvelope env = Handler.Operators();

        Assert.Equal(3, env.Operators.Count);

        OperatorDto caged = env.Operators.Single(o => o.Family == "caged");
        OperatorParamDto shape = caged.Params.Single(p => p.Name == "shape");
        Assert.Equal("enum", shape.Kind);
        Assert.Equal(5, shape.Values!.Count);
        Assert.Equal("E", shape.Default);
        Assert.Contains(caged.Params, p => p.Kind == "region");

        // CAGED covers a triad on all 5 shapes; a shell does not cover triads at all.
        Assert.Contains(caged.EligibleShapesByQuality, q => q.Quality == "Major" && q.Shapes.Count == 5);
        OperatorDto shell = env.Operators.Single(o => o.Family == "shell");
        Assert.DoesNotContain(shell.EligibleShapesByQuality, q => q.Quality == "Major");
        Assert.Contains(shell.EligibleShapesByQuality, q => q.Quality == "Dominant7" && q.Shapes.Count == 2);
    }
}

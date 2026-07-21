using ChordFlow.Bridge;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Rhythm.Generation;

namespace ChordFlow.Features.Rhythm;

/// <summary>
/// Maps a wire <see cref="RhythmGenerationRequest"/> onto the Core <see cref="GenerationParams"/> union — the
/// one place that knows the token vocabulary (kind / selection / behaviour tokens; random palette). Every
/// unknown token or out-of-range count fails loud as a <see cref="FormatException"/>, which the handler
/// surfaces as a <c>rhythmGenerateError</c> (never a host crash). 4/4 only (req EX5).
/// </summary>
public static class RhythmRequestResolver
{
    /// <summary>Resolve <paramref name="r"/> to a Core generation request.</summary>
    /// <exception cref="FormatException">A token is unknown or a count is out of range.</exception>
    public static GenerationParams Resolve(RhythmGenerationRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);
        return Normalize(r.Strategy) switch
        {
            "pattern" => ResolvePattern(r),
            "random" => ResolveRandom(r),
            _ => throw new FormatException($"Unknown rhythm strategy '{r.Strategy}' (expected pattern/random)."),
        };
    }

    // --- Pattern strategy (v2: kind + selection + behaviours) --------------

    private static PatternParams ResolvePattern(RhythmGenerationRequest r)
    {
        int barCount = r.BarCount ?? 1;
        if (barCount is < 1 or > 4)
        {
            throw new FormatException($"BarCount {barCount} is out of range (1–4).");
        }

        var behaviours = (r.Behaviours ?? Array.Empty<RhythmBehaviourSpec>()).Select(ResolveBehaviour).ToArray();
        return new PatternParams(
            ResolveKind(r.Kind), ResolveSelection(r.Selection), behaviours, barCount, TimeSignature.FourFour, r.Seed);
    }

    private static RhythmKind ResolveKind(RhythmKindSpec? spec)
    {
        if (spec is null)
        {
            throw new FormatException("A pattern needs a kind.");
        }

        return Normalize(spec.Source) switch
        {
            "density" => RhythmKind.Density(spec.Subdivision ?? 2, spec.OnsetCount ?? 1),
            "placement" => RhythmKind.Placement(spec.Subdivision ?? 2, Normalize(spec.Region) is "" ? "all" : Normalize(spec.Region), spec.OnsetCount ?? 1),
            "figure" => GrooveFigures.ById(spec.FigureId ?? "")
                ?? throw new FormatException($"Unknown groove figure '{spec.FigureId}'."),
            _ => throw new FormatException($"Unknown kind source '{spec.Source}' (expected density/placement/figure)."),
        };
    }

    private static PatternSelection ResolveSelection(RhythmSelectionSpec? spec)
    {
        if (spec is null)
        {
            return new PatternSelection.Fixed(0);
        }

        return Normalize(spec.Kind) switch
        {
            "fixed" => new PatternSelection.Fixed(spec.Index ?? 0),
            "cycle" => new PatternSelection.Cycle(),
            "randominkind" or "random" => new PatternSelection.RandomInKind(),
            "fixedplusrotating" => new PatternSelection.FixedPlusRotating(spec.Index ?? 0),
            _ => throw new FormatException($"Unknown selection '{spec.Kind}'."),
        };
    }

    private static SequenceBehaviour ResolveBehaviour(RhythmBehaviourSpec spec)
    {
        return Normalize(spec.Kind) switch
        {
            "displace" => new SequenceBehaviour.Displace(Arg(spec, 0)),
            "sweep" => new SequenceBehaviour.Sweep(),
            "restbar" => new SequenceBehaviour.RestBar(ArgOr(spec, 0, 1), ArgOr(spec, 1, 1)),
            "callresponse" => new SequenceBehaviour.CallResponse(),
            _ => throw new FormatException($"Unknown behaviour '{spec.Kind}'."),
        };
    }

    // --- Random strategy ---------------------------------------------------

    private static RandomParams ResolveRandom(RhythmGenerationRequest r)
    {
        if (r.Palette is null || r.Palette.Count == 0)
        {
            throw new FormatException("The random strategy needs a non-empty value palette.");
        }

        int content = r.ContentBars ?? 1;
        int silence = r.SilenceBars ?? 0;
        if (content is < 1 or > 4)
        {
            throw new FormatException($"ContentBars {content} is out of range (1–4).");
        }

        if (silence is < 0 or > 4)
        {
            throw new FormatException($"SilenceBars {silence} is out of range (0–4).");
        }

        double rest = r.RestProbability ?? 0.0;
        if (rest is < 0.0 or > 1.0)
        {
            throw new FormatException($"RestProbability {rest} is out of range (0–1).");
        }

        return new RandomParams(r.Palette, content, silence, TimeSignature.FourFour, r.Seed, rest);
    }

    private static int Arg(RhythmBehaviourSpec spec, int index) =>
        spec.Args is { } a && index < a.Count
            ? a[index]
            : throw new FormatException($"Behaviour '{spec.Kind}' needs argument {index}.");

    private static int ArgOr(RhythmBehaviourSpec spec, int index, int fallback) =>
        spec.Args is { } a && index < a.Count ? a[index] : fallback;

    private static string Normalize(string? token) => token?.Trim().ToLowerInvariant() ?? string.Empty;
}

using ChordFlow.Bridge;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Rhythm.Generation;

namespace ChordFlow.Features.Rhythm;

/// <summary>
/// Maps a wire <see cref="RhythmGenerationRequest"/> (strategy + operator/behaviour/family/palette tokens)
/// onto the Core <see cref="GenerationParams"/> discriminated union — the one place that knows the token
/// vocabulary. Every unknown token or out-of-range count fails loud as a <see cref="FormatException"/>, which
/// the handler surfaces as a <c>rhythmGenerateError</c> (never a host crash). 4/4 only (req EX5).
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

    private static PatternParams ResolvePattern(RhythmGenerationRequest r)
    {
        int barCount = r.BarCount ?? 1;
        if (barCount is < 1 or > 4)
        {
            throw new FormatException($"BarCount {barCount} is out of range (1–4).");
        }

        return new PatternParams(
            ResolveFamily(r.Family),
            ResolveOperator(r.Operator),
            ResolveBehaviour(r.Behaviour),
            barCount,
            TimeSignature.FourFour,
            r.Seed);
    }

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

    private static RhythmFamily ResolveFamily(string? token) => Normalize(token) switch
    {
        "quarter" => RhythmFamily.Quarter,
        "eighth" => RhythmFamily.Eighth,
        _ => throw new FormatException($"Unknown rhythm family '{token}' (expected quarter/eighth)."),
    };

    private static BarOperator ResolveOperator(RhythmOperatorSpec? spec)
    {
        if (spec is null)
        {
            throw new FormatException("A pattern needs an operator.");
        }

        return Normalize(spec.Kind) switch
        {
            "uniform" => new BarOperator.Uniform(),
            "isolate" => new BarOperator.Isolate(Arg(spec, 0)),
            "anchorrotate" => new BarOperator.AnchorRotate(),
            "mask" => new BarOperator.Mask(Args(spec)),
            "displace" => new BarOperator.Displace(Arg(spec, 0)),
            "accumulate" => new BarOperator.Accumulate(Arg(spec, 0)),
            "thin" => new BarOperator.Thin(Arg(spec, 0)),
            _ => throw new FormatException($"Unknown bar operator '{spec.Kind}'."),
        };
    }

    private static SequenceBehaviour ResolveBehaviour(RhythmBehaviourSpec? spec)
    {
        if (spec is null)
        {
            throw new FormatException("A pattern needs a behaviour.");
        }

        return Normalize(spec.Kind) switch
        {
            "repeat" => new SequenceBehaviour.Repeat(),
            "cycle" => new SequenceBehaviour.Cycle(),
            "sweep" => new SequenceBehaviour.Sweep(),
            "restbar" => new SequenceBehaviour.RestBar(ArgOr(spec, 0, 1), ArgOr(spec, 1, 1)),
            "callresponse" => new SequenceBehaviour.CallResponse(),
            _ => throw new FormatException($"Unknown sequence behaviour '{spec.Kind}'."),
        };
    }

    private static int Arg(RhythmOperatorSpec spec, int index) =>
        spec.Args is { } a && index < a.Count
            ? a[index]
            : throw new FormatException($"Operator '{spec.Kind}' needs argument {index}.");

    private static IReadOnlyList<int> Args(RhythmOperatorSpec spec) =>
        spec.Args is { Count: > 0 } a
            ? a
            : throw new FormatException($"Operator '{spec.Kind}' needs at least one argument.");

    private static int ArgOr(RhythmBehaviourSpec spec, int index, int fallback) =>
        spec.Args is { } a && index < a.Count ? a[index] : fallback;

    private static string Normalize(string? token) => token?.Trim().ToLowerInvariant() ?? string.Empty;
}

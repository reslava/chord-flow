namespace ChordFlow.Bridge;

/// <summary>
/// One bar-operator choice on a <see cref="RhythmGenerationRequest"/> — a <see cref="Kind"/> token
/// (<c>uniform</c>/<c>isolate</c>/<c>anchorRotate</c>/<c>mask</c>/<c>displace</c>/<c>accumulate</c>/<c>thin</c>)
/// plus its integer <see cref="Args"/> (e.g. <c>isolate</c> → <c>[beat]</c>, <c>mask</c> → the beat set,
/// <c>displace</c>/<c>accumulate</c>/<c>thin</c> → <c>[n]</c>). The resolver maps this to the Core
/// <c>BarOperator</c> discriminated union.
/// </summary>
public sealed record RhythmOperatorSpec(string Kind, IReadOnlyList<int>? Args = null);

/// <summary>
/// One sequence-behaviour choice — a <see cref="Kind"/> token (<c>repeat</c>/<c>cycle</c>/<c>sweep</c>/
/// <c>restBar</c>/<c>callResponse</c>) plus optional integer <see cref="Args"/> (<c>restBar</c> →
/// <c>[contentBars, restBars]</c>). Maps to the Core <c>SequenceBehaviour</c> union.
/// </summary>
public sealed record RhythmBehaviourSpec(string Kind, IReadOnlyList<int>? Args = null);

/// <summary>
/// The wire request for one <c>rhythmGenerate</c> verb (the Rhythm Generator dogfood page): the
/// <see cref="Strategy"/> discriminator (<c>pattern</c>/<c>random</c>), the reproducibility <see cref="Seed"/>,
/// the drum <see cref="Voice"/> the onset grid projects onto for preview (default closed hi-hat),
/// <see cref="Tempo"/> BPM, and the per-strategy params. Pattern uses <see cref="Family"/> +
/// <see cref="Operator"/> + <see cref="Behaviour"/> + <see cref="BarCount"/>; Random uses
/// <see cref="Palette"/> (alphaTex note values) + <see cref="ContentBars"/> + <see cref="SilenceBars"/>. The
/// Features resolver turns the tokens into a Core <c>GenerationParams</c>; unknown tokens fail loud as a
/// <c>rhythmGenerateError</c>.
/// </summary>
public sealed record RhythmGenerationRequest(
    string Strategy,
    int Seed,
    string? Voice,
    int Tempo,
    // Pattern strategy
    string? Family,
    RhythmOperatorSpec? Operator,
    RhythmBehaviourSpec? Behaviour,
    int? BarCount,
    // Random strategy
    IReadOnlyList<int>? Palette,
    int? ContentBars,
    int? SilenceBars,
    // Random rest density (0..1, req IN12) + the play-time reference pulse ("off"/"beat1", req IN8) — the
    // latter a non-generated reference layer the handler adds, not part of the generated grid.
    double? RestProbability = null,
    string? ReferencePulse = null);

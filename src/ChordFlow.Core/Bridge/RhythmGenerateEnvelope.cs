namespace ChordFlow.Bridge;

/// <summary>
/// How bars are drawn from the kind: <c>fixed</c> (+<c>Index</c>) / <c>cycle</c> (+<c>Index</c> = start) /
/// <c>randomInKind</c> / <c>fixedPlusRotating</c> (+<c>Index</c> = fixed, +<c>RotatingIndex</c> = rotating start).
/// </summary>
public sealed record RhythmSelectionSpec(string Kind, int? Index = null, int? RotatingIndex = null);

/// <summary>
/// One multi-bar behaviour overlay — a <c>Kind</c> token (<c>displace</c>/<c>sweep</c>/<c>restBar</c>/
/// <c>callResponse</c>) plus optional integer <c>Args</c> (<c>displace</c> → <c>[cells]</c>, <c>restBar</c> →
/// <c>[content, rest]</c>). Maps to the Core <c>SequenceBehaviour</c> union.
/// </summary>
public sealed record RhythmBehaviourSpec(string Kind, IReadOnlyList<int>? Args = null);

/// <summary>
/// The wire request for one <c>rhythmGenerate</c> verb (the Rhythm Generator page). <see cref="Strategy"/> is
/// one of three: <c>figure</c> (a named <see cref="FigureId"/>), <c>pattern</c> (a placement family —
/// <see cref="Subdivision"/> × <see cref="Region"/> all/onbeat/offbeat × <see cref="OnsetCount"/>), or
/// <c>random</c> (<see cref="Palette"/> + <see cref="ContentBars"/>/<see cref="SilenceBars"/> +
/// <see cref="RestProbability"/>). Figure/pattern share <see cref="Selection"/> + <see cref="Behaviours"/> +
/// <see cref="BarCount"/> (1–16). The Features resolver maps this to a Core <c>GenerationParams</c>; unknown
/// tokens fail loud as a <c>rhythmGenerateError</c>.
/// </summary>
public sealed record RhythmGenerationRequest(
    string Strategy,
    int Seed,
    string? Voice,
    int Tempo,
    // figure strategy
    string? FigureId,
    // pattern (placement) strategy
    int? Subdivision,
    string? Region,
    int? OnsetCount,
    // figure + pattern share these
    RhythmSelectionSpec? Selection,
    IReadOnlyList<RhythmBehaviourSpec>? Behaviours,
    int? BarCount,
    // random strategy
    IReadOnlyList<int>? Palette,
    int? ContentBars,
    int? SilenceBars,
    // Random rest density (0..1, req IN12) + the play-time reference pulse ("off"/"beat1", req IN8).
    double? RestProbability = null,
    string? ReferencePulse = null);

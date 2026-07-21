namespace ChordFlow.Bridge;

/// <summary>
/// The kind of bar patterns for a Pattern-strategy request (design §3a v2): a generated family
/// (<c>Source</c> = <c>density</c> → <c>Subdivision</c>+<c>OnsetCount</c>; <c>placement</c> →
/// +<c>Region</c> on-beat/off-beat/all) or a curated <c>figure</c> (<c>FigureId</c> from the catalog).
/// </summary>
public sealed record RhythmKindSpec(
    string Source, int? Subdivision = null, string? Region = null, int? OnsetCount = null, string? FigureId = null);

/// <summary>
/// How bars are drawn from the kind: <c>fixed</c> (+<c>Index</c>) / <c>cycle</c> / <c>randomInKind</c> /
/// <c>fixedPlusRotating</c> (+<c>Index</c>).
/// </summary>
public sealed record RhythmSelectionSpec(string Kind, int? Index = null);

/// <summary>
/// One multi-bar behaviour overlay — a <c>Kind</c> token (<c>displace</c>/<c>sweep</c>/<c>restBar</c>/
/// <c>callResponse</c>) plus optional integer <c>Args</c> (<c>displace</c> → <c>[cells]</c>, <c>restBar</c> →
/// <c>[content, rest]</c>). Maps to the Core <c>SequenceBehaviour</c> union.
/// </summary>
public sealed record RhythmBehaviourSpec(string Kind, IReadOnlyList<int>? Args = null);

/// <summary>
/// The wire request for one <c>rhythmGenerate</c> verb (the Rhythm Generator dogfood page): the
/// <see cref="Strategy"/> discriminator (<c>pattern</c>/<c>random</c>), the reproducibility <see cref="Seed"/>,
/// the drum <see cref="Voice"/> the onset grid projects onto, <see cref="Tempo"/> BPM, and the per-strategy
/// params. Pattern uses <see cref="Kind"/> + <see cref="Selection"/> + <see cref="Behaviours"/> +
/// <see cref="BarCount"/> (design §3a v2); Random uses <see cref="Palette"/> + <see cref="ContentBars"/> +
/// <see cref="SilenceBars"/> + <see cref="RestProbability"/>. The Features resolver turns the tokens into a
/// Core <c>GenerationParams</c>; unknown tokens fail loud as a <c>rhythmGenerateError</c>.
/// </summary>
public sealed record RhythmGenerationRequest(
    string Strategy,
    int Seed,
    string? Voice,
    int Tempo,
    // Pattern strategy (v2)
    RhythmKindSpec? Kind,
    RhythmSelectionSpec? Selection,
    IReadOnlyList<RhythmBehaviourSpec>? Behaviours,
    int? BarCount,
    // Random strategy
    IReadOnlyList<int>? Palette,
    int? ContentBars,
    int? SilenceBars,
    // Random rest density (0..1, req IN12) + the play-time reference pulse ("off"/"beat1", req IN8) — the
    // latter a non-generated reference layer the handler adds, not part of the generated grid.
    double? RestProbability = null,
    string? ReferencePulse = null);

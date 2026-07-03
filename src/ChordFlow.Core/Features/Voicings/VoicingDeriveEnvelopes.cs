using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// Outbound bridge envelopes (C#→JS) for the Voicings Engine inspector page (voicings-engine, req IN11/IN16). The
/// page sends a <c>voicingDerive</c> request (operator + quality + root + params) and gets a
/// <see cref="VoicingDerivationEnvelope"/>: the abstract tone selection, the ordered "show your work" steps, and the
/// realized grip diagram — one round-trip. It also fetches the operator catalog once (<c>voicingOperators</c> →
/// <see cref="VoicingOperatorsEnvelope"/>) so the controls are schema-driven, never hardcoded. On invalid input the
/// host replies with <see cref="VoicingDeriveErrorEnvelope"/> (fail-loud, UI-safe).
/// </summary>

/// <summary>One selected chord tone of the abstract voicing: its interval, a spelled interval label (e.g. "b7"), the function, and the note name in key.</summary>
public sealed record ToneSelectionDto(int Interval, string IntervalLabel, string Function, string Note);

/// <summary>One ordered derivation step: its <paramref name="Kind"/> (for styling) + the rendered <paramref name="Label"/>.</summary>
public sealed record RealizationStepDto(string Kind, string Label);

/// <summary>The full derivation reply: <c>{"type":"voicingDerivation", …}</c> — the abstract voicing, the steps, and the realized grip diagram.</summary>
public sealed record VoicingDerivationEnvelope(
    string Id,
    string Family,
    string Kind,
    IReadOnlyList<ToneSelectionDto> ToneSelection,
    IReadOnlyList<RealizationStepDto> RealizationSteps,
    FretboardDiagram Diagram,
    string Type = "voicingDerivation");

/// <summary>A voicingDerive failure (bad family/quality/shape or an ineligible combo): <c>{"type":"voicingDeriveError","message":…}</c>.</summary>
public sealed record VoicingDeriveErrorEnvelope(string Message, string Type = "voicingDeriveError");

/// <summary>One declared operator parameter — an enum choice (<c>values</c> + <c>default</c>) or the neck region (<c>min</c>/<c>max</c>).</summary>
public sealed record OperatorParamDto(
    string Name, string Kind, IReadOnlyList<string>? Values, string? Default, int? Min, int? Max);

/// <summary>The shapes/forms an operator offers for one quality — so the page narrows the shape picker per quality.</summary>
public sealed record QualityShapesDto(string Quality, IReadOnlyList<string> Shapes);

/// <summary>One operator in the catalog: identity, kind, declared params, and the eligible (quality → shapes) coverage.</summary>
public sealed record OperatorDto(
    string Family,
    string Kind,
    string DisplayName,
    IReadOnlyList<OperatorParamDto> Params,
    IReadOnlyList<QualityShapesDto> EligibleShapesByQuality);

/// <summary>The operator catalog reply: <c>{"type":"voicingOperators","operators":[…]}</c> — drives the inspector's schema-based controls.</summary>
public sealed record VoicingOperatorsEnvelope(IReadOnlyList<OperatorDto> Operators, string Type = "voicingOperators");

using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The neck fret window a voicing is placed in — the region whose lowest occurrence of the root anchors the grip.
/// <see cref="Default"/> is <c>[0, 15]</c> (mirrors <c>VoicingGridHandler.NeckMaxFret</c>): every root's lowest
/// octave anchor lands within the first 12 frets, so the grip's own span is the effective window.
/// </summary>
public readonly record struct FretRegion(int MinFret, int MaxFret)
{
    /// <summary>The default full-neck search window <c>[0, 15]</c>.</summary>
    public static readonly FretRegion Default = new(0, 15);
}

/// <summary>
/// A request to a <see cref="IVoicingOperator"/> — the universal axes every operator takes: the
/// <see cref="Quality"/>, the <see cref="Root"/>, the neck <see cref="Region"/>, and the operator's declared enum
/// <see cref="Params"/> (validated against its <see cref="ParameterSchema"/>). Quality/root/region are the request;
/// the operator-specific knobs (CAGED shape, shell form) live in <see cref="Params"/>.
/// </summary>
public sealed record VoicingRequest(
    Quality Quality,
    PitchClass Root,
    FretRegion Region,
    ParameterValues Params)
{
    /// <summary>A request over the default full-neck region with the given enum parameters.</summary>
    public static VoicingRequest For(Quality quality, PitchClass root, ParameterValues @params) =>
        new(quality, root, FretRegion.Default, @params);
}

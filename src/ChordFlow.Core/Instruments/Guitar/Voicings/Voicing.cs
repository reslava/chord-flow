
namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// How a chord is fingered: the set of fretted positions to strike together, plus optional
/// diagram hints for the alphaTex <c>\chord (...)</c> directive. The hints are <b>presentation
/// metadata</b>, not positional data — the <see cref="Positions"/> list stays authoritative.
/// </summary>
/// <param name="Positions">The fretted notes of the voicing.</param>
/// <param name="BarreFret">Fret of a barre across multiple strings, if any.</param>
/// <param name="FirstFret">Lowest fret shown on the diagram (the diagram's nut position).</param>
/// <param name="MutedStrings">String numbers (1 = high E .. 6 = low E) that are muted/not played.</param>
public sealed record Voicing(
    IReadOnlyList<FretPosition> Positions,
    int? BarreFret = null,
    int? FirstFret = null,
    IReadOnlyList<int>? MutedStrings = null);

namespace ChordFlow.Domain;

/// <summary>
/// An authored voicing entry: a CAGED chord shape captured at the canonical <b>C</b> anchor and
/// inherently movable. The parser normalizes any declared anchor down to its lowest non-negative C
/// placement, so each <c>(Quality, Shape)</c> has a single canonical form — "open" is just where a
/// movable shape lands, never a separate kind. <see cref="VoicingRealizer.Realize"/> slides
/// <see cref="Canonical"/> to any root.
/// </summary>
/// <param name="Quality">The chord quality the book matches on.</param>
/// <param name="Shape">The CAGED family (diagram labelling + ranked-list tiebreak).</param>
/// <param name="RootString">alphaTab string number (6 = low E .. 1 = high E) sounding the root.</param>
/// <param name="Canonical">The C-anchored voicing — absolute frets at C.</param>
public sealed record VoicingShape(
    Quality Quality,
    CagedShape Shape,
    int RootString,
    Voicing Canonical);

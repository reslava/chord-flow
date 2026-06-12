namespace ChordFlow.Rendering;

/// <summary>
/// A tuplet marker on a <see cref="RhythmSlot"/>: <see cref="Numerator"/> notes in the time of
/// <see cref="Denominator"/> — e.g. <c>(3, 2)</c> for a triplet (three in the time of two). The renderer
/// emits it as the verified alphaTex <c>{tu N}</c> beat effect (N = <see cref="Numerator"/>); both the
/// eighth-triplet (<c>:8</c>) and 16th-triplet (<c>:16</c>) grids carry <c>(3, 2)</c> — the note value
/// distinguishes them.
/// </summary>
public readonly record struct Tuplet(int Numerator, int Denominator);

/// <summary>
/// One quantized note/rest cell produced by <see cref="RhythmQuantizer"/>: a single representable
/// note value (<see cref="NoteValue"/> = the alphaTex <c>:N</c> number — 1/2/4/8/16) that is either a
/// rest or a struck beat, optionally tied to the previous slot when a sustained note was split across
/// the grid. <see cref="StartTick"/> is the slot's bar-relative onset tick — the renderer uses it to
/// look up which <c>ChordSpan</c> the slot falls under (<c>HarmonicBar.SpanCovering</c>).
/// <see cref="Tuplet"/> is set when the slot sits on a triplet grid (null for straight beats). The
/// renderer turns a slot into a <c>:N</c> token plus a chord group or <c>r</c>, suffixed with
/// <c>{tu N}</c> when tupled.
/// </summary>
public readonly record struct RhythmSlot(
    int NoteValue, bool IsRest, bool TiedToPrevious, int StartTick, Tuplet? Tuplet = null);

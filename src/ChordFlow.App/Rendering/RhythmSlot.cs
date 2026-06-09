namespace ChordFlow.Rendering;

/// <summary>
/// One quantized note/rest cell produced by <see cref="RhythmQuantizer"/>: a single representable
/// note value (<see cref="NoteValue"/> = the alphaTex <c>:N</c> number — 1/2/4/8/16) that is either a
/// rest or a struck beat, optionally tied to the previous slot when a sustained note was split across
/// the grid. <see cref="StartTick"/> is the slot's bar-relative onset tick — the renderer uses it to
/// look up which <c>ChordSpan</c> the slot falls under (<c>HarmonicBar.SpanCovering</c>). The renderer
/// turns a slot into a <c>:N</c> token plus a chord group or <c>r</c>.
/// </summary>
public readonly record struct RhythmSlot(int NoteValue, bool IsRest, bool TiedToPrevious, int StartTick);

namespace ChordFlow.Rendering;

/// <summary>
/// One quantized note/rest cell produced by <see cref="RhythmQuantizer"/>: a single representable
/// note value (<see cref="NoteValue"/> = the alphaTex <c>:N</c> number — 1/2/4/8/16) that is either a
/// rest or a struck beat, optionally tied to the previous slot when a sustained note was split across
/// the grid. The renderer turns a slot into a <c>:N</c> token plus a chord group or <c>r</c>.
/// </summary>
public readonly record struct RhythmSlot(int NoteValue, bool IsRest, bool TiedToPrevious);

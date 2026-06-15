namespace ChordFlow.Domain;

/// <summary>
/// A presentation-ready chord-diagram model for one <see cref="VoicingShape"/> — computed in the kernel so the
/// JS fret-box renderer stays a dumb drawer (IN6: theory lives in Core). It captures, per string, what to draw
/// (muted / open / a fretted dot) and how to label/color it (the note name, its interval against the chord root,
/// and the chord-tone function that drives the dot color). Strings are ordered low-E(6) → high-E(1).
/// </summary>
/// <param name="FirstFret">Lowest fret in the diagram window (the nut position the JS draws from).</param>
/// <param name="BarreFret">Fret of a barre across strings, if any.</param>
/// <param name="Strings">Six entries, low-E(6) → high-E(1).</param>
public sealed record DiagramModel(
    int FirstFret,
    int? BarreFret,
    IReadOnlyList<DiagramString> Strings);

/// <summary>
/// One string of a <see cref="DiagramModel"/>. <see cref="State"/> is <c>muted</c> (no note), <c>open</c>
/// (fret 0, sounding), or <c>fretted</c>. Sounding strings (open or fretted) carry the spelled
/// <see cref="Note"/>, the <see cref="Interval"/> label (<c>R</c>/<c>b3</c>/<c>5</c>/<c>b7</c>…), and the
/// <see cref="Function"/> color key (<c>root</c>/<c>third</c>/<c>fifth</c>/<c>seventh</c>/<c>tension</c>); a
/// muted string leaves all three null.
/// </summary>
public sealed record DiagramString(
    int String,
    string State,
    int? Fret,
    string? Note,
    string? Interval,
    string? Function);

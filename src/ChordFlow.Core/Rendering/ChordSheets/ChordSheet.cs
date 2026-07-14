using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Rendering.ChordSheets;

/// <summary>
/// The Core-computed carrier the <c>ChordFlowChordSheet</c> JS view draws — the <b>page</b> twin of the
/// alphaTex string (<see cref="AlphaTexRenderer"/>) and the fretboard diagram (<see cref="FretboardDiagram"/>).
/// A chord sheet is a projection of a realized <c>Song</c>/<c>Progression</c>: sections → rows → cells →
/// chord(s), plus the header metadata. All music theory is resolved here (constraint C1); the JS is a dumb
/// drawer that picks which carried fields to paint (letter vs Nashville vs Roman, note vs interval, layout,
/// theme) with no re-fetch.
/// <para>
/// The model is <b>instrument-agnostic</b> apart from the optional <see cref="ChordRef.Diagram"/> — a guitar
/// <see cref="FretboardDiagram"/> — which is why the type lives in <c>Rendering/</c> (the presentation/export
/// seam, where <c>Rendering → Instruments</c> is an allowed edge) rather than the instrument-free
/// <c>Music/</c> kernel.
/// </para>
/// </summary>
/// <param name="Header">Title / artist / key / tempo / feel / time-signature / capo.</param>
/// <param name="Sections">The ordered sections (Intro, Verse, …) each holding rows of bar cells.</param>
public sealed record ChordSheet(
    ChordSheetHeader Header,
    IReadOnlyList<ChordSheetSection> Sections);

/// <summary>
/// The sheet's header block. <see cref="KeyName"/> is the <b>sounding</b> key (spelled tonic of the realized
/// progression), shown as "key of X"; it is display metadata, not a transposition instruction (the chords are
/// already realized). <see cref="Capo"/> mirrors <c>Song.Capo</c> (null = no capo). <see cref="Feel"/> is the
/// <c>TripletFeel</c> ident or null.
/// </summary>
public sealed record ChordSheetHeader(
    string Title,
    string? Artist,
    string KeyName,
    int? Tempo,
    string? Feel,
    string TimeSig,
    int? Capo);

/// <summary>
/// One labelled section of the arrangement (its <see cref="Label"/> is the boxed tag — "Verse"/"A"/…, or
/// null when the source has no section name). Rows are chunked at the render's <c>barsPerRow</c> so the model
/// already reflects the printed line breaks.
/// </summary>
public sealed record ChordSheetSection(
    string? Label,
    IReadOnlyList<ChordSheetRow> Rows);

/// <summary>A printed row of bar cells (<c>barsPerRow</c> of them, the last row possibly short).</summary>
public sealed record ChordSheetRow(
    IReadOnlyList<ChordSheetCell> Cells);

/// <summary>
/// One bar. Normally holds one-or-more <see cref="Chords"/> (a multi-chord bar splits the cell by each
/// chord's <see cref="ChordRef.DurationTicks"/> against <see cref="BarTicks"/>). When
/// <see cref="RepeatOfPrev"/> is set the bar repeats the previous bar's harmony and is drawn as a simile
/// <c>%</c>; <see cref="Chords"/> is then empty. The repeat detection (bar-equality) is done in Core so the
/// JS only prints the glyph (req IN13).
/// </summary>
public sealed record ChordSheetCell(
    IReadOnlyList<ChordRef> Chords,
    bool RepeatOfPrev,
    int BarTicks);

/// <summary>
/// One chord in a cell, carrying <b>every notation the view might show</b> so a notation/label toggle needs no
/// round-trip (req IN6/C3): the concrete symbol, the Nashville degree, and the diatonic Roman function. The
/// tone strip and the optional fret diagram are the two "below-cell" adornments (req IN10).
/// </summary>
/// <param name="Concrete">Conventional symbol in the sounding key (<c>C</c> / <c>Fmaj7</c> / <c>F/C</c>) — <see cref="ChordSymbol.Format"/>.</param>
/// <param name="Degree">Nashville scale-degree token (<c>1</c> / <c>5-</c> / <c>#4</c>) from the <c>RomanDegree</c>.</param>
/// <param name="Roman">Diatonic Roman function (<c>I</c> / <c>V7</c> / <c>ii</c>). In v1 this is the honest diatonic degree only — no secondary-dominant/borrowed inference (that arrives from the harmonic-analysis thread; req IN7/EX2).</param>
/// <param name="DurationTicks">The chord's span length in 48-PPQ ticks; its share of <see cref="ChordSheetCell.BarTicks"/> gives the cell-split proportion.</param>
/// <param name="Tones">The chord's spelled tones for the note-name ⇄ interval-degree tone strip.</param>
/// <param name="Diagram">The comped voicing as a fret diagram, present only when the diagram adornment is on; else null.</param>
public sealed record ChordRef(
    string Concrete,
    string Degree,
    string Roman,
    int DurationTicks,
    IReadOnlyList<ChordSheetTone> Tones,
    FretboardDiagram? Diagram);

/// <summary>
/// One spelled chord tone for the tone-strip adornment. Carries both the <see cref="Note"/> name and the
/// <see cref="Interval"/> degree so the strip's label toggle needs no re-fetch (the same both-labels-carried
/// idea as <see cref="FretboardMarker"/>). <see cref="Function"/> is the colour key, reusing FretR's palette
/// vocabulary (<c>root</c>/<c>third</c>/<c>fifth</c>/<c>sixth</c>/<c>seventh</c>/<c>tension</c>).
/// </summary>
public sealed record ChordSheetTone(
    string Note,
    string Interval,
    string Function);

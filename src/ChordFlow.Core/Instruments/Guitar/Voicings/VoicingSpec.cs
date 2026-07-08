namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// A parsed voicing-spec — the shared value grammar behind a per-chord <c>{…}</c> annotation and a
/// Song <c>voice &lt;selector&gt; = …</c> default (req <c>IN1</c>–<c>IN4</c>). Exactly one concrete form:
/// a literal <see cref="GripSpec"/> or a source-qualified <see cref="ReferenceSpec"/>. This is a purely
/// <b>syntactic</b> value — the authored frets are kept verbatim; the movable normalize-to-C + transpose
/// to a sounding root is a realization concern (<see cref="VoicingRealizer"/>).
/// </summary>
public abstract record VoicingSpec;

/// <summary>
/// A literal custom grip: the six authored frets (low-E→high-E) with an optional <see cref="Anchor"/>
/// declaring where the root sits — needed only for rootless/ambiguous shapes (req <c>IN3</c>/<c>IN11</c>).
/// Sugar-equivalent to a <c>c:</c>-prefixed grip.
/// </summary>
/// <param name="Positions">The fretted strings, verbatim at the authored position (not yet normalized to C).</param>
/// <param name="MutedStrings">String numbers (6 = low E .. 1 = high E) that are muted (<c>x</c>).</param>
/// <param name="Anchor">The <c>root:</c> clause, if present.</param>
public sealed record GripSpec(
    IReadOnlyList<FretPosition> Positions,
    IReadOnlyList<int> MutedStrings,
    GripAnchor? Anchor = null) : VoicingSpec;

/// <summary>
/// A source-qualified reference to a listed voicing: <c>&lt;Source&gt;:&lt;Id&gt;</c> where the source is
/// <c>u</c> (user), <c>a</c> (automatic / engine-derived), or a package id (req <c>IN2</c>). The source is
/// kept verbatim; interpreting it against the stores/engine is resolution's job (req <c>IN6</c>).
/// </summary>
public sealed record ReferenceSpec(string Source, string Id) : VoicingSpec;

/// <summary>
/// The <c>root:&lt;string&gt;[@&lt;fret&gt;]</c> anchor of a grip (req <c>C9</c>). <see cref="Fret"/> is
/// <c>null</c> for a <b>voiced</b> root (the fret is read from the grip on that string) and set for a
/// <b>phantom</b> root on a muted string — the form that makes rootless voicings expressible.
/// </summary>
/// <param name="String">alphaTab string number (6 = low E .. 1 = high E) sounding (or implying) the root.</param>
/// <param name="Fret">The phantom fret, or <c>null</c> when the root is a sounded string.</param>
public sealed record GripAnchor(int String, int? Fret = null);

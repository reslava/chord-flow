namespace ChordFlow.Rendering;

/// <summary>
/// The render-time presentation options threaded into <see cref="IScoreRenderer.Render"/>. The diagram/name
/// toggles are <b>content-kind</b> options — they change the alphaTex the renderer emits, so flipping one
/// requires a re-render. <see cref="Voicing"/> is the transient comping <b>voicing source</b> (the practice
/// knob) consumed by the Features comping resolver to build the <see cref="CompingPlan"/> — the renderer
/// itself no longer selects voicings (engine-derived-as-app-source D4=(B)).
/// <para>
/// The type is optional everywhere it is accepted; an absent <see cref="RenderOptions"/> coalesces to
/// <see cref="Default"/>, which reproduces the pre-options presentation exactly.
/// </para>
/// </summary>
/// <param name="ShowChordNames">Emit a chord-name label at each chord change.</param>
/// <param name="ShowChordDiagramsOverStaff">Show chord diagrams (fret boxes) inline above the staff (alphaTex <c>\chordDiagramsInScore</c>).</param>
/// <param name="ShowChordDiagramsOnTop">Show the chord-diagram list at the top of the score (alphaTex <c>\chordDiagramsOnTop</c>).</param>
/// <param name="Voicing">The comping voicing source (main-source + region + ranking); null ⇒ <see cref="VoicingSource.Default"/>.</param>
public sealed record RenderOptions(
    bool ShowChordNames = false,
    bool ShowChordDiagramsOverStaff = false,
    bool ShowChordDiagramsOnTop = false,
    VoicingSource? Voicing = null)
{
    /// <summary>The neutral options — what an absent <see cref="RenderOptions"/> means.</summary>
    public static readonly RenderOptions Default = new();

    /// <summary>The chosen voicing source, or <see cref="VoicingSource.Default"/> when unset.</summary>
    public VoicingSource VoicingOrDefault => Voicing ?? VoicingSource.Default;
}

/// <summary>
/// The comping voicing source — a transient generate-time practice knob (engine-derived-as-app-source, req
/// IN6), <b>not</b> baked into content. Names the <see cref="Kind"/> (<c>automatic</c> engine / <c>package</c>
/// / <c>user</c>); for <c>automatic</c> it carries the neck <see cref="MinFret"/>/<see cref="MaxFret"/> region
/// and the <see cref="Ranking"/> strategy; for <c>package</c> the <see cref="PackageId"/>. The comping
/// resolver tries this source per chord, then falls back <c>user &gt; package &gt; automatic</c>. Absent ⇒
/// <see cref="Default"/> (automatic, full neck, Closest).
/// </summary>
public sealed record VoicingSource(
    string Kind = "automatic",
    int? MinFret = null,
    int? MaxFret = null,
    string? PackageId = null,
    string? Ranking = null)
{
    /// <summary>Engine-derived voicings.</summary>
    public const string Automatic = "automatic";

    /// <summary>A content pack's voicings (<see cref="PackageId"/> names which).</summary>
    public const string Package = "package";

    /// <summary>User-authored voicings.</summary>
    public const string User = "user";

    /// <summary>Full-neck top fret for the automatic region when <see cref="MaxFret"/> is unset.</summary>
    public const int FullNeckMaxFret = 15;

    /// <summary>The neutral source: automatic, full neck, Closest ranking.</summary>
    public static readonly VoicingSource Default = new();

    /// <summary>The region's low fret (0 when unset).</summary>
    public int RegionMinFret => MinFret ?? 0;

    /// <summary>The region's high fret (full neck when unset).</summary>
    public int RegionMaxFret => MaxFret ?? FullNeckMaxFret;
}

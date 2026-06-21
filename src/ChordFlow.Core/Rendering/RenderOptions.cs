namespace ChordFlow.Rendering;

/// <summary>
/// The render-time presentation options threaded into <see cref="IScoreRenderer.Render(Exercise, RenderOptions?)"/>.
/// These are <b>content-kind</b> toggles — they change the alphaTex the renderer emits, so flipping one
/// requires a re-render (unlike player-kind options such as metronome/count-in, which the JS render
/// component applies via the alphaTab API and never reach Core).
/// <para>
/// The type is optional everywhere it is accepted; an absent <see cref="RenderOptions"/> coalesces to
/// <see cref="Default"/>, which reproduces the pre-options render exactly (backward-compatible).
/// </para>
/// </summary>
/// <param name="ShowChordNames">Emit a chord-name label at each chord change.</param>
/// <param name="ShowChordDiagramsOverStaff">Show chord diagrams (fret boxes) inline above the staff (alphaTex <c>\chordDiagramsInScore</c>).</param>
/// <param name="ShowChordDiagramsOnTop">Show the chord-diagram list at the top of the score (alphaTex <c>\chordDiagramsOnTop</c>).</param>
/// <param name="Voicing">How a chord resolves to a voicing at render time. v1 ships only
/// <see cref="VoicingStrategy.ByDifficulty"/> — the existing difficulty-keyed selection.</param>
public sealed record RenderOptions(
    bool ShowChordNames = false,
    bool ShowChordDiagramsOverStaff = false,
    bool ShowChordDiagramsOnTop = false,
    VoicingStrategy Voicing = VoicingStrategy.ByDifficulty)
{
    /// <summary>The neutral options — what an absent <see cref="RenderOptions"/> means (today's render).</summary>
    public static readonly RenderOptions Default = new();
}

/// <summary>
/// Render-time voicing-selection strategy. v1 ships only <see cref="ByDifficulty"/>; CAGED-shape-specific
/// preference is deferred to the <c>caged-system</c> / <c>voicings</c> domain threads. The enum exists so the
/// seam is ready — an unimplemented value fails loud rather than silently falling back.
/// </summary>
public enum VoicingStrategy
{
    /// <summary>Resolve each chord via the existing <c>VoicingBook.Lookup(chord, difficulty)</c> selection.</summary>
    ByDifficulty = 0,
}

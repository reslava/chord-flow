using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The <b>canonical-C</b> voicing producer of the general <see cref="FretboardDiagram"/> carrier — the
/// music-theory side of the voicing preview (IN5/IN6/IN7). It is the C-anchored special case of
/// <see cref="RealizedVoicingDiagram.Build"/>: a <see cref="VoicingShape"/> is shown at the canonical-C anchor
/// (EX2: no root-picker in v1; movability — showing a chord at its real root — is <see cref="RealizedVoicingDiagram"/>).
/// </summary>
public static class VoicingDiagram
{
    private static readonly Key CAnchor = new(new PitchClass(0), IsMinor: false);

    /// <summary>Compute the fretboard diagram for <paramref name="shape"/> at its canonical-C anchor.</summary>
    public static FretboardDiagram Build(VoicingShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        // The canonical voicing is authored at root C, so realize it as a C-rooted chord through the general
        // real-root producer — one marker-building path, no drift.
        return RealizedVoicingDiagram.Build(new Chord(new PitchClass(0), shape.Quality), shape.Canonical, CAnchor);
    }
}

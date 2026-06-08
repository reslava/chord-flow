using ChordFlow.Domain;

namespace ChordFlow.Rendering;

/// <summary>
/// Renders an <see cref="Exercise"/> to a score-notation string. The seam that keeps
/// future exporters (MIDI / Guitar Pro / MusicXML) additive — alphaTex is just the
/// first implementation.
/// </summary>
public interface IScoreRenderer
{
    string Render(Exercise exercise);
}

using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Rhythm;
using ChordFlow.Rendering;

namespace ChordFlow.Features.Drums;

/// <summary>
/// Drums vertical slice: the one handler behind the <c>drumPreview</c> bridge verb (the Drums dogfood page).
/// Parses the hit-grid DSL into a <see cref="DrumGroove"/>, then projects it two ways from that single parse
/// — the alphaTex percussion track (<see cref="DrumGrooveRenderer"/>) and the grid model
/// (<see cref="DrumGrooveDiagram"/>). Stateless and pure (no db); a bad DSL throws <see cref="FormatException"/>,
/// which the host maps to a <see cref="DrumPreviewErrorEnvelope"/> (mirrors the scale/CRUD parse-error path).
/// </summary>
public sealed class DrumGroovePreviewHandler
{
    private readonly DrumGrooveRenderer _renderer = new();

    /// <summary>Parse <paramref name="dsl"/> and render it at <paramref name="tempo"/> BPM (4/4).</summary>
    /// <exception cref="FormatException">The hit-grid DSL is malformed (see <see cref="DrumGrooveParser"/>).</exception>
    public DrumPreviewEnvelope Preview(string dsl, int tempo)
    {
        ArgumentNullException.ThrowIfNull(dsl);
        DrumGroove groove = DrumGrooveParser.Parse("preview", "Drums", dsl, TimeSignature.FourFour);
        string tex = _renderer.Render(groove, tempo);
        return new DrumPreviewEnvelope(tex, DrumGrooveDiagram.Build(groove));
    }
}

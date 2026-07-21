using ChordFlow.Bridge;
using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Rhythm.Generation;
using ChordFlow.Rendering;

namespace ChordFlow.Features.Rhythm;

/// <summary>
/// Rhythm-generation vertical slice: the one handler behind the <c>rhythmGenerate</c> bridge verb (the
/// Rhythm Generator dogfood page). Resolves the request to a Core <see cref="GenerationParams"/>, generates
/// the <see cref="OnsetGrid"/>, projects it to a single-voice <see cref="DrumGroove"/>, and returns the
/// percussion tex (<see cref="DrumGrooveRenderer"/>) + grid model (<see cref="DrumGrooveDiagram"/>) + an
/// onset-ASCII debug string — all from that one generated grid, so they cannot drift. Stateless and pure (no
/// db). Bad input throws <see cref="FormatException"/>, which the host maps to a
/// <see cref="RhythmGenerateErrorEnvelope"/> (mirrors the <c>drumPreview</c> path, req C3 — no new alphaTex code).
/// </summary>
public sealed class RhythmGenerateHandler
{
    private readonly DrumGrooveRenderer _renderer = new();

    /// <summary>Generate and project <paramref name="request"/> into the preview reply.</summary>
    /// <exception cref="FormatException">A token is unknown or a count is out of range.</exception>
    public RhythmGeneratedEnvelope Generate(RhythmGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        GenerationParams parameters = RhythmRequestResolver.Resolve(request);
        OnsetGrid grid = RhythmGenerator.Generate(parameters);
        DrumVoice voice = ResolveVoice(request.Voice);
        DrumGroove groove = OnsetGridToDrumGroove.Project(grid, voice);
        if (IsBeat1Reference(request.ReferencePulse))
        {
            groove = WithBeat1Reference(groove, voice, grid.TimeSignature.BeatTicks);
        }

        string tex = _renderer.Render(groove, request.Tempo <= 0 ? 100 : request.Tempo);
        // The ASCII grid shows the GENERATED rhythm only (not the reference layer).
        return new RhythmGeneratedEnvelope(tex, DrumGrooveDiagram.Build(groove), RenderGridText(grid));
    }

    private static bool IsBeat1Reference(string? token) =>
        string.Equals(token?.Trim(), "beat1", StringComparison.OrdinalIgnoreCase);

    // Layer a NON-generated reference click on beat 1 of every bar in a distinct voice (so it reads as its own
    // DrumsR row and sounds as a downbeat anchor). Never part of the generated grid (req IN8/IN12).
    private static DrumGroove WithBeat1Reference(DrumGroove groove, DrumVoice generatedVoice, int beatTicks)
    {
        DrumVoice refVoice = generatedVoice == DrumVoice.Kick ? DrumVoice.HiHatPedal : DrumVoice.Kick;
        var refLane = new DrumLane(refVoice, new[] { RhythmEvent.Hit(0, beatTicks) });
        var bars = groove.Bars
            .Select(bar => new DrumBar(bar.Lanes.Append(refLane).ToArray()))
            .ToArray();
        return groove with { Bars = bars };
    }

    private static DrumVoice ResolveVoice(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return DrumVoice.HiHatClosed;
        }

        string trimmed = token.Trim();
        if (DrumVoices.TryParse(trimmed, out DrumVoice voice))
        {
            return voice;
        }

        if (Enum.TryParse(trimmed, ignoreCase: true, out DrumVoice parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new FormatException($"Unknown drum voice '{token}'.");
    }

    // Onset ASCII of the whole grid: per beat, x = attack cell, . = rest cell; beats space-separated, bars '|'.
    private static string RenderGridText(OnsetGrid grid) =>
        string.Join(" | ", grid.Bars.Select(bar => string.Join(" ", bar.Beats.Select(BeatText))));

    private static string BeatText(Block block)
    {
        var cells = new char[block.Subdivision];
        for (int k = 0; k < block.Subdivision; k++)
        {
            cells[k] = block.Onsets.Contains(k) ? 'x' : '.';
        }

        return new string(cells);
    }
}

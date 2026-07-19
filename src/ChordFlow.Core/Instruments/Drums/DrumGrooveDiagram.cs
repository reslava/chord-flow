namespace ChordFlow.Instruments.Drums;

/// <summary>
/// One hit on the drum grid: a bar-relative onset. <see cref="Bar"/> is the 0-based bar index, matching the
/// renderer's / alphaTab's master-bar axis so a playback marker can line up. <see cref="Tick"/> is the
/// bar-relative onset on the 48-PPQ grid.
/// </summary>
public readonly record struct DrumGrooveHit(int Bar, int Tick);

/// <summary>
/// One row of the drum grid — a voice lane spanning the whole groove. <see cref="Label"/> is the short DSL
/// token (e.g. <c>HH</c>) DrumsR draws; <see cref="Voice"/> is the enum (a colour/ordering key). Hits are in
/// bar-then-tick order.
/// </summary>
public sealed record DrumGrooveLaneRow(string Label, DrumVoice Voice, IReadOnlyList<DrumGrooveHit> Hits);

/// <summary>
/// The spatial model a JS <b>DrumsR</b> draws — the drums twin of <c>FretboardDiagram</c> (IN4). A DUMB
/// drawer consumes it: all structure is computed here in Core, the JS only lays out rows × a time axis and
/// animates a marker off the shared playback beat/position bus (IN6/C1 — no music theory in JS). Voice-major
/// (one row per voice, first-seen order) — the transpose of the bar-major <see cref="DrumGroove"/> — with the
/// bar/beat/tick geometry the view needs for gridlines and the marker.
/// </summary>
public sealed record DrumGrooveDiagram(
    string Title,
    IReadOnlyList<DrumGrooveLaneRow> Lanes,
    int BarCount,
    int BeatsPerBar,
    int TicksPerBar)
{
    /// <summary>
    /// Build the grid model from <paramref name="groove"/>: transpose bar-major lanes into one voice-major
    /// row per distinct voice (first-seen order), gathering each voice's hits across every bar.
    /// </summary>
    public static DrumGrooveDiagram Build(DrumGroove groove)
    {
        ArgumentNullException.ThrowIfNull(groove);

        var lanes = new List<DrumGrooveLaneRow>();
        foreach (DrumVoice voice in groove.DistinctVoices())
        {
            var hits = new List<DrumGrooveHit>();
            for (int barIndex = 0; barIndex < groove.Bars.Count; barIndex++)
            {
                DrumLane? lane = groove.Bars[barIndex].Lanes.FirstOrDefault(l => l.Voice == voice);
                if (lane is null)
                {
                    continue;
                }

                foreach (var hit in lane.Events)
                {
                    hits.Add(new DrumGrooveHit(barIndex, hit.Position));
                }
            }

            lanes.Add(new DrumGrooveLaneRow(voice.Token(), voice, hits));
        }

        return new DrumGrooveDiagram(
            groove.Name, lanes, groove.Bars.Count, groove.TimeSignature.Numerator, groove.TimeSignature.BarTicks);
    }
}

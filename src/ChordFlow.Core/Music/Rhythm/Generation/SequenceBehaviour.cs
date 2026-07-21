namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// A multi-bar overlay applied to each selected bar pattern in order (design §3a v2) — a composable list on
/// <see cref="PatternParams"/>. <b>Displace</b> shifts every bar's onsets a fixed amount; <b>Sweep</b> shifts
/// bar N by N cells (walk a figure through every metric position — the signature drill); <b>RestBar</b> replaces
/// bars in the rest window with silence; <b>CallResponse</b> silences the odd "your-turn" bars.
/// </summary>
public abstract record SequenceBehaviour
{
    /// <summary>Transform the bar at <paramref name="barIndex"/> (already drawn by the selection).</summary>
    public abstract OnsetBar Apply(int barIndex, OnsetBar bar, int beatsPerBar);

    /// <summary>Shift every bar's onsets <see cref="Cells"/> cells later (offbeat/pushed variants).</summary>
    public sealed record Displace(int Cells) : SequenceBehaviour
    {
        public override OnsetBar Apply(int barIndex, OnsetBar bar, int beatsPerBar) =>
            new DisplaceTransform(Cells).Apply(bar);
    }

    /// <summary>Shift bar N by N cells — the same shape felt against every metric position.</summary>
    public sealed record Sweep : SequenceBehaviour
    {
        public override OnsetBar Apply(int barIndex, OnsetBar bar, int beatsPerBar) =>
            new DisplaceTransform(barIndex).Apply(bar);
    }

    /// <summary>Within each cycle of <see cref="ContentBars"/> + <see cref="RestBars"/>, silence the rest bars.</summary>
    public sealed record RestBar(int ContentBars = 1, int RestBars = 1) : SequenceBehaviour
    {
        public override OnsetBar Apply(int barIndex, OnsetBar bar, int beatsPerBar) =>
            barIndex % (ContentBars + RestBars) < ContentBars ? bar : OnsetBar.Rest(beatsPerBar);
    }

    /// <summary>Content bar (call), then an empty "your turn" bar (response), alternating.</summary>
    public sealed record CallResponse : SequenceBehaviour
    {
        public override OnsetBar Apply(int barIndex, OnsetBar bar, int beatsPerBar) =>
            barIndex % 2 == 0 ? bar : OnsetBar.Rest(beatsPerBar);
    }
}

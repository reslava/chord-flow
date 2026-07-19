using System.Globalization;
using System.Text;
using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Rhythm;

namespace ChordFlow.Rendering;

/// <summary>
/// Renders a <see cref="DrumGroove"/> to an alphaTex <b>percussion track</b> (req IN3). The drums peer of
/// <see cref="AlphaTexRenderer"/> and, like it, the only code that knows alphaTex syntax. Kept a
/// <b>concrete</b> renderer (no polymorphic <c>IInstrument</c> yet — req C7); the shared instrument seam is
/// extracted later (<c>chordflow/instrument-rendering</c>) once this and the guitar path can be diffed.
/// <para>
/// Output: header <c>\instrument percussion</c> + <c>\articulation defaults</c> + <c>\tempo</c>/<c>\ts</c>
/// (no <c>\ks</c> — percussion is keyless), then bars of stateful <c>:N</c> durations whose beats are the
/// drum articulation names, <b>simultaneous hits grouped in <c>( )</c></b>, and <c>r</c> where nothing
/// sounds. Per bar the lanes are merged into one onset timeline (each onset → the set of voices striking
/// there); the gaps between onsets are quantized by the shared <see cref="RhythmQuantizer"/>, so triplet
/// (<c>{tu 3}</c>), dotted (<c>{d}</c>) and coalesced rests all fall out of the one rhythm model (req C2).
/// A groove is standalone — no song/key/harmony (req C6).
/// </para>
/// </summary>
public sealed class DrumGrooveRenderer
{
    /// <summary>Render <paramref name="groove"/> to alphaTex at <paramref name="tempo"/> BPM.</summary>
    public string Render(DrumGroove groove, int tempo)
    {
        ArgumentNullException.ThrowIfNull(groove);
        if (groove.Bars.Count == 0)
        {
            throw new ArgumentException("Cannot render a groove with no bars.", nameof(groove));
        }

        TimeSignature ts = groove.TimeSignature;
        var sb = new StringBuilder();

        sb.Append("\\title \"").Append(groove.Name).Append("\"\n");
        sb.Append("\\tempo ").Append(tempo.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("\\instrument percussion\n");
        sb.Append("\\articulation defaults\n");
        sb.Append("\\ts ").Append(ts.Numerator).Append(' ').Append(ts.Denominator).Append('\n');
        sb.Append(".\n");

        var state = new RenderState();
        var barLines = new List<string>(groove.Bars.Count);
        foreach (DrumBar bar in groove.Bars)
        {
            barLines.Add(RenderBar(bar, ts, state));
        }

        sb.Append(string.Join("\n", barLines));
        return sb.ToString();
    }

    /// <summary>
    /// Render <paramref name="barCount"/> alphaTex bar strings for the groove tiled cyclically beneath a
    /// progression (<c>drums-under-a-song</c> IN3): master bar i uses groove bar <c>i % m</c>, sharing the
    /// stateful <c>:N</c> duration across every bar. Just the bar bodies — no header/track wrapper; the caller
    /// (<see cref="AlphaTexRenderer"/>) composes them into a percussion <c>\track</c>. A single-bar groove
    /// (m = 1) repeats on every bar. <paramref name="barCount"/> ≤ 0 yields an empty list.
    /// </summary>
    public IReadOnlyList<string> RenderTiledBars(DrumGroove groove, int barCount)
    {
        ArgumentNullException.ThrowIfNull(groove);
        if (groove.Bars.Count == 0)
        {
            throw new ArgumentException("Cannot render a groove with no bars.", nameof(groove));
        }

        var state = new RenderState();
        var barLines = new List<string>(Math.Max(0, barCount));
        for (int i = 0; i < barCount; i++)
        {
            barLines.Add(RenderBar(groove.Bars[i % groove.Bars.Count], groove.TimeSignature, state));
        }

        return barLines;
    }

    // One bar: merge lanes into an onset→voices timeline, quantize it, then format each slot. A note slot
    // emits its onset's articulation group (order-stable); a rest slot emits "r".
    private static string RenderBar(DrumBar bar, TimeSignature ts, RenderState state)
    {
        // tick → the voices striking at that tick (ordered by DrumVoice) + the finest cell width among them.
        var byTick = new SortedDictionary<int, Onset>();
        foreach (DrumLane lane in bar.Lanes)
        {
            foreach (RhythmEvent hit in lane.Events)
            {
                if (byTick.TryGetValue(hit.Position, out Onset onset))
                {
                    onset.Voices.Add(lane.Voice);
                    byTick[hit.Position] = onset with { CellTicks = Math.Min(onset.CellTicks, hit.Length) };
                }
                else
                {
                    byTick[hit.Position] = new Onset(new SortedSet<DrumVoice> { lane.Voice }, hit.Length);
                }
            }
        }

        // A percussion hit is instantaneous: notate it at its own cell width (capped to the gap before the
        // next attack), and let the shared quantizer coalesce the remaining silence into rests — so a sparse
        // groove reads as hits + rests rather than over-sustained notes, and dense grids stay one note per cell.
        int[] onsets = byTick.Keys.ToArray();
        var events = new List<RhythmEvent>(onsets.Length);
        for (int i = 0; i < onsets.Length; i++)
        {
            int gap = (i + 1 < onsets.Length ? onsets[i + 1] : ts.BarTicks) - onsets[i];
            events.Add(RhythmEvent.Hit(onsets[i], Math.Min(byTick[onsets[i]].CellTicks, gap)));
        }

        IReadOnlyList<RhythmSlot> slots = RhythmQuantizer.Quantize(events, ts);

        // Note slots map 1:1 to onsets in order (no ties/boundaries → one slot per synthetic note).
        var tokens = new List<string>(slots.Count);
        int onsetIndex = 0;
        foreach (RhythmSlot slot in slots)
        {
            string prefix = string.Empty;
            string durationToken = slot.NoteValue.ToString(CultureInfo.InvariantCulture);
            if (durationToken != state.CurrentDuration)
            {
                prefix = ":" + durationToken + " ";
                state.CurrentDuration = durationToken;
            }

            string body = slot.IsRest
                ? "r"
                : VoiceGroup(byTick[onsets[onsetIndex++]].Voices);

            var effects = new List<string>(2);
            if (slot.Dotted)
            {
                effects.Add("d");
            }

            if (slot.Tuplet is { } tuplet)
            {
                effects.Add("tu " + tuplet.Numerator.ToString(CultureInfo.InvariantCulture));
            }

            string effectGroup = effects.Count > 0 ? "{" + string.Join(" ", effects) + "}" : string.Empty;
            tokens.Add(prefix + body + effectGroup);
        }

        return string.Join(" ", tokens) + " |";
    }

    // The alphaTex beat for a set of simultaneous hits: a lone articulation name, or "(A B …)" for a chord
    // of hits (verified drum syntax — notes in parentheses sound together).
    private static string VoiceGroup(SortedSet<DrumVoice> voices)
    {
        if (voices.Count == 1)
        {
            return voices.Min.Articulation();
        }

        return "(" + string.Join(" ", voices.Select(v => v.Articulation())) + ")";
    }

    // A merged onset: the voices striking together at one tick + the finest authoring cell width among them
    // (the notated hit length before rests fill the remainder). Voices is mutable (a set built as lanes merge).
    private readonly record struct Onset(SortedSet<DrumVoice> Voices, int CellTicks);

    // The alphaTex ":N" duration persists across beats and bars until it changes (stateful, like the guitar
    // renderer). {tu}/{d} do not persist, so they are re-emitted per slot from the RhythmSlot.
    private sealed class RenderState
    {
        public string? CurrentDuration;
    }
}

using ChordFlow.Music.Harmony;

namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// The default comping-ranking strategy (engine-derived-as-app-source, req IN7) — minimal-movement comping:
/// <list type="bullet">
/// <item>the <b>first</b> chord takes the lowest-fret grip in the region;</item>
/// <item>a chord that has <b>already appeared</b> reuses its earlier grip (muscle memory — same chord, same grip);</item>
/// <item>otherwise the candidate closest to the previous grip, measured by the <b>full per-string
///   fret-distance sum</b> over the strings sounding in both grips (D6).</item>
/// </list>
/// Strings sounding in only one of the two grips are skipped from the distance in slice 1 (the simplest
/// rule; revisit if it picks visibly jumpy grips). Lowest-fret breaks a distance tie.
/// </summary>
public sealed class ClosestRanking : IVoicingRanking
{
    public Voicing Pick(Chord chord, IReadOnlyList<Voicing> candidates, VoicingRankingContext context)
    {
        ArgumentNullException.ThrowIfNull(chord);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(context);
        if (candidates.Count == 0)
        {
            throw new ArgumentException("Closest ranking needs at least one candidate.", nameof(candidates));
        }

        Voicing chosen;
        if (context.PreviousGrip is null)
        {
            chosen = candidates.OrderBy(LowestFret).First();
        }
        else if (context.ChosenByChord.TryGetValue(chord, out Voicing? earlier))
        {
            chosen = earlier;
        }
        else
        {
            Voicing previous = context.PreviousGrip;
            chosen = candidates
                .OrderBy(c => Distance(previous, c))
                .ThenBy(LowestFret)
                .First();
        }

        context.ChosenByChord[chord] = chosen;
        context.PreviousGrip = chosen;
        return chosen;
    }

    // The lowest fret a grip is played at — its FirstFret hint, else the lowest fretted position.
    private static int LowestFret(Voicing voicing) =>
        voicing.FirstFret ?? voicing.Positions.Min(p => p.Fret);

    // Sum of |Δfret| over strings sounding in BOTH grips; strings unique to one grip are skipped (slice 1).
    private static int Distance(Voicing a, Voicing b)
    {
        var aByString = a.Positions.ToDictionary(p => p.String, p => p.Fret);
        int sum = 0;
        foreach (FretPosition p in b.Positions)
        {
            if (aByString.TryGetValue(p.String, out int fret))
            {
                sum += Math.Abs(fret - p.Fret);
            }
        }

        return sum;
    }
}

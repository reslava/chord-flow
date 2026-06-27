using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Progressions;
using ChordFlow.Music.Songs;
using ChordFlow.Persistence;
using ChordFlow.Rendering;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// Resolves the comping grip for every chord of a realized song into a <see cref="CompingPlan"/>
/// (engine-derived-as-app-source, req IN4/D4=(B); shell-voicing-derivation IN7) — the Features-layer pass that
/// makes the renderer a pure formatter. Per chord it tries the chosen <b>main source</b>, else falls back
/// <c>user &gt; package &gt; automatic</c>; the surviving candidates are picked by the source's ranking strategy
/// (Closest by default). <c>automatic</c> candidates are derived on the fly in the chosen voicing
/// <b>family</b> (<see cref="VoicingFamily"/>) over the catalog's shapes for the chord's quality, adapted to
/// <see cref="Voicing"/>s — never stored (C3). A family with no grip for a quality (e.g. a shell of a triad)
/// falls back to the <c>caged</c> family before the source fallback chain.
/// </summary>
public static class CompingResolver
{
    /// <summary>Build the comping plan for <paramref name="song"/> using <paramref name="main"/> (+ its fallback) and <paramref name="stored"/>.</summary>
    public static CompingPlan Resolve(RealizedSong song, VoicingSource main, IStoredVoicingSource stored)
    {
        ArgumentNullException.ThrowIfNull(song);
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(stored);

        IVoicingRanking ranking = RankingFor(main.Ranking);
        var context = new VoicingRankingContext();
        var candidateCache = new Dictionary<Chord, IReadOnlyList<Voicing>>();
        var plan = new Dictionary<Chord, Voicing>();

        foreach (RealizedSection section in song.Sections)
        {
            foreach (RealizedBar bar in section.Bars)
            {
                foreach (RealizedSpan span in bar.Spans)
                {
                    Chord chord = span.Chord;
                    if (!candidateCache.TryGetValue(chord, out IReadOnlyList<Voicing>? candidates))
                    {
                        candidates = CandidatesFor(chord, main, stored);
                        if (candidates.Count == 0)
                        {
                            throw new InvalidOperationException(
                                $"No voicing source can comp {chord.Root.Value}:{chord.Quality} (main '{main.Kind}', fallback user/package/automatic all empty).");
                        }

                        candidateCache[chord] = candidates;
                    }

                    plan[chord] = ranking.Pick(chord, candidates, context);
                }
            }
        }

        return new CompingPlan(plan);
    }

    // Main source first; on empty, the fixed fallback chain user > package > automatic (skipping the main, and
    // any tier yielding nothing) — so a song can mix sources per chord.
    private static IReadOnlyList<Voicing> CandidatesFor(Chord chord, VoicingSource main, IStoredVoicingSource stored)
    {
        VoicingFamily family = ResolveFamily(main.Family);

        IReadOnlyList<Voicing> candidates = FromKind(chord, main.Kind, main.PackageId, main, family, stored);
        if (candidates.Count > 0)
        {
            return candidates;
        }

        foreach (string tier in new[] { VoicingSource.User, VoicingSource.Package, VoicingSource.Automatic })
        {
            if (string.Equals(tier, main.Kind, StringComparison.Ordinal))
            {
                continue;
            }

            candidates = FromKind(chord, tier, packageId: null, main, family, stored);
            if (candidates.Count > 0)
            {
                return candidates;
            }
        }

        return Array.Empty<Voicing>();
    }

    private static IReadOnlyList<Voicing> FromKind(
        Chord chord, string kind, string? packageId, VoicingSource main, VoicingFamily family,
        IStoredVoicingSource stored) => kind switch
        {
            VoicingSource.Automatic => AutomaticCandidates(chord, main.RegionMinFret, main.RegionMaxFret, family),
            VoicingSource.Package => stored.Candidates(chord, ContentSource.Package, packageId),
            VoicingSource.User => stored.Candidates(chord, ContentSource.User, null),
            _ => throw new FormatException($"Unknown voicing source kind '{kind}'."),
        };

    // Derive the chosen family's grip for every catalog shape of the chord's quality within [min,max]. If the
    // requested family covers no shape for this quality (e.g. a triad under `shell`), fall back to the always-
    // present `caged` family for this chord before the source fallback chain (IN7).
    private static IReadOnlyList<Voicing> AutomaticCandidates(Chord chord, int minFret, int maxFret, VoicingFamily family)
    {
        IReadOnlyList<Voicing> candidates = DeriveFamily(chord, family, minFret, maxFret);
        if (candidates.Count == 0 && family != VoicingFamily.Caged)
        {
            candidates = DeriveFamily(chord, VoicingFamily.Caged, minFret, maxFret);
        }

        return candidates;
    }

    // Per shape: derive the family grip and adapt to a Voicing. A shape that cannot be cleanly placed/spelled at
    // its lowest occurrence in this region simply isn't a candidate here — that is the region filter, not a
    // failure (Derive signals it with InvalidOperationException / ArgumentOutOfRangeException). Fail-loud (C2) is
    // preserved at CandidatesFor: if NO source yields a grip for the chord, resolution throws.
    private static IReadOnlyList<Voicing> DeriveFamily(Chord chord, VoicingFamily family, int minFret, int maxFret)
    {
        var candidates = new List<Voicing>();
        foreach (CagedShape shape in CagedVoicingCatalog.ShapesFor(family, chord.Quality))
        {
            try
            {
                ChordShape derived = FamilyVoicing.Derive(family, chord.Quality, shape, chord.Root, minFret, maxFret);
                candidates.Add(ChordShapeVoicing.ToVoicing(derived));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
            {
                // No clean grip for this shape in this region — skip it as a candidate.
            }
        }

        return candidates;
    }

    // Map the source's family token (null ⇒ caged) to the enum.
    private static VoicingFamily ResolveFamily(string? family) =>
        VoicingFamilies.TryParse((family ?? VoicingFamily.Caged.Token()).Trim().ToLowerInvariant(), out VoicingFamily f)
            ? f
            : throw new FormatException($"Unknown voicing family '{family}'.");

    // Only Closest ships in this thread; the variety / voice-leading modes are voicing-ranking-strategies.
    private static IVoicingRanking RankingFor(string? ranking) =>
        (ranking ?? "closest").Trim().ToLowerInvariant() switch
        {
            "closest" => new ClosestRanking(),
            _ => throw new FormatException($"Unknown voicing ranking '{ranking}'."),
        };
}

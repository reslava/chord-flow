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
    /// <summary>
    /// Build the comping plan for <paramref name="song"/> using <paramref name="main"/> (+ its fallback),
    /// <paramref name="stored"/>, and <paramref name="references"/> for source-qualified voicing references.
    /// Per span the most-specific-wins cascade (explicit-voicing-reference IN5) applies before the ranking fill:
    /// a per-chord <c>{…}</c> annotation (a per-occurrence override) › the Song's degree-scoped <c>voice</c>
    /// default › its quality-scoped <c>voice</c> default › today's candidate/ranking fill. Explicit tiers fail
    /// loud (IN6) when a spec is malformed or a reference can't be resolved. The fill path is unchanged.
    /// </summary>
    public static CompingPlan Resolve(
        RealizedSong song, VoicingSource main, IStoredVoicingSource stored, IVoicingReferenceSource? references = null)
    {
        ArgumentNullException.ThrowIfNull(song);
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(stored);
        references ??= VoicingReferenceSource.Empty;

        IVoicingRanking ranking = RankingFor(main.Ranking);
        var context = new VoicingRankingContext();
        var candidateCache = new Dictionary<Chord, IReadOnlyList<Voicing>>();
        var plan = new Dictionary<Chord, Voicing>();                    // per chord value: voice default or ranking fill
        var spanOverrides = new Dictionary<RealizedSpan, Voicing>();    // per occurrence: {…} annotations

        foreach (RealizedSection section in song.Sections)
        {
            foreach (RealizedBar bar in section.Bars)
            {
                foreach (RealizedSpan span in bar.Spans)
                {
                    Chord chord = span.Chord;

                    // Tier 1 — a per-chord {…} annotation: a per-occurrence override (never leaks to other spans).
                    if (span.VoicingAnnotation is { } annotation)
                    {
                        if (!spanOverrides.TryGetValue(span, out Voicing? overridden))
                        {
                            overridden = ResolveSpec(annotation, span, references, $"annotation \"{{{annotation}}}\"");
                            spanOverrides[span] = overridden;
                        }

                        context.PreviousGrip = overridden;
                        continue;
                    }

                    // Tiers 2–4 — per chord value: a degree/quality `voice` default, else the ranking fill.
                    if (!plan.TryGetValue(chord, out Voicing? chosen))
                    {
                        chosen = ResolveVoiceDefault(span, song.Voices, references)
                                 ?? Fill(chord, main, stored, ranking, context, candidateCache);
                        plan[chord] = chosen;
                    }

                    context.PreviousGrip = chosen;
                }
            }
        }

        return new CompingPlan(plan, spanOverrides);
    }

    // The Song's `voice` default for this span, most-specific first: a degree-scoped selector (`voice 17`)
    // beats a quality-scoped one (`voice *7`). Null when the Song declares neither for this chord.
    private static Voicing? ResolveVoiceDefault(
        RealizedSpan span, IReadOnlyDictionary<VoiceSelector, string> voices, IVoicingReferenceSource references)
    {
        if (voices.TryGetValue(VoiceSelector.ForDegree(span.Degree), out string? degreeSpec))
        {
            return ResolveSpec(degreeSpec, span, references, $"voice default for {span.Degree.Degree}{span.Degree.Quality}");
        }

        if (voices.TryGetValue(VoiceSelector.ForQuality(span.Chord.Quality), out string? qualitySpec))
        {
            return ResolveSpec(qualitySpec, span, references, $"voice default for *{span.Chord.Quality}");
        }

        return null;
    }

    // Parse an opaque voicing-spec (a grip or a reference) and realize it at the span's chord root. Fail loud
    // (IN6) on a malformed spec or an unresolvable reference/grip — the spec was carried verbatim from Music.
    private static Voicing ResolveSpec(
        string specText, RealizedSpan span, IVoicingReferenceSource references, string what)
    {
        VoicingSpec spec = VoicingDslParser.ParseSpec(specText);
        Voicing? voicing = spec switch
        {
            GripSpec grip => VoicingRealizer.RealizeGrip(grip, span.Chord.Root),
            ReferenceSpec reference => references.Resolve(reference.Source, reference.Id, span.Chord),
            _ => null,
        };

        return voicing
            ?? throw new InvalidOperationException(
                $"Voicing {what} could not be resolved for {span.Chord.Root.Value}:{span.Chord.Quality}.");
    }

    // Today's ranking fill for a chord value (the tier-4 default), factored out so the cascade can fall through.
    private static Voicing Fill(
        Chord chord, VoicingSource main, IStoredVoicingSource stored, IVoicingRanking ranking,
        VoicingRankingContext context, Dictionary<Chord, IReadOnlyList<Voicing>> candidateCache)
    {
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

        return ranking.Pick(chord, candidates, context);
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

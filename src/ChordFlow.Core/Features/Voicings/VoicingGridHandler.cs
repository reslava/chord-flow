using ChordFlow.Bridge;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;
using ChordFlow.Rendering;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// GuitarVoicingsR vertical slice: the one handler behind the <c>voicingGrid</c> bridge verb. It filters the engine
/// catalog by the faceted filter state, realizes each surviving <c>(family, quality, shape)</c> combo at the chosen
/// root, and returns the whole grid in one round-trip (C4) as ordered <see cref="VoicingGridCell"/>s.
/// <para>
/// <b>Combos are the single source of truth</b> (<see cref="CagedVoicingCatalog"/>, C3) realized via the shared
/// <see cref="FamilyVoicing"/> → <see cref="RealizedVoicingDiagram"/> path — no parallel catalog/realizer. Today only
/// the <c>automatic</c> source produces cells; the <c>package</c>/<c>user</c> tiers stay in the wire shape but yield
/// nothing until a stored-combo enumeration source lands (the "empty filter cells until derived" model, EX6).
/// </para>
/// <para>
/// <b>Filter semantics</b> (faceted, multi-select): each level's array is the set of <i>enabled</i> tokens. A cell is
/// kept iff its token is enabled in <b>every</b> level (OR within a level via set membership, AND across levels). A
/// <c>null</c> level is unconstrained (matches all) — so a bare <c>{root}</c> request returns the whole grid; an empty
/// <c>[]</c> level admits nothing. An empty result is an empty cell list, never an error (C5). Stateless and pure.
/// </para>
/// </summary>
public sealed class VoicingGridHandler
{
    // Auto-region search bound (mirrors CagedChordHandler): every root's lowest octave anchor lands within the first
    // 12 frets, so [0, 15] always finds the lowest placement; the grip's own span is the reach window.
    private const int NeckMaxFret = 15;

    /// <summary>Filter the catalog by <paramref name="filter"/>, realize each surviving combo at the root, and build the grid.</summary>
    public VoicingGridResultEnvelope Build(VoicingGridFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // Source facet — only `automatic` yields cells today (Option A). If the filter excludes it, the grid is empty.
        if (!LevelMatches(filter.Sources, VoicingSource.Automatic))
        {
            return new VoicingGridResultEnvelope(Array.Empty<VoicingGridCell>());
        }

        var root = new PitchClass(((filter.Root % 12) + 12) % 12);
        var key = new Key(root, IsMinor: false); // spells the cell note names for this root

        var cells = new List<VoicingGridCell>();
        foreach ((VoicingFamily family, Quality quality, CagedShape shape) in OrderedCombos())
        {
            if (!LevelMatches(filter.Families, family.Token()) || !FacetsMatch(filter, quality))
            {
                continue;
            }

            if (TryRealize(family, quality, shape, root, key) is { } diagram)
            {
                cells.Add(new VoicingGridCell(
                    AutomaticVoicingId.For(family, quality, shape),
                    EngineVoicingSource.DisplayName(family, quality, shape),
                    family.Token(),
                    quality.ToString(),
                    shape.ToString(),
                    diagram));
            }
        }

        return new VoicingGridResultEnvelope(cells);
    }

    // Catalog combos reordered rows-by-quality then (within a row) by family then shape/form (IN5). The catalog's own
    // order is family-major; the grid lays out qualities as rows, so quality leads.
    private static IEnumerable<(VoicingFamily Family, Quality Quality, CagedShape Shape)> OrderedCombos() =>
        CagedVoicingCatalog.Combos
            .OrderBy(c => (int)c.Quality)
            .ThenBy(c => (int)c.Family)
            .ThenBy(c => (int)c.Shape);

    // A cell's quality passes iff each of its three facet tokens is enabled in its level (3rd × 5th × 7th).
    private static bool FacetsMatch(VoicingGridFilter filter, Quality quality)
    {
        QualityFacets facets = QualityFacets.Of(quality);
        return LevelMatches(filter.Thirds, facets.ThirdToken)
            && LevelMatches(filter.Fifths, facets.FifthToken)
            && LevelMatches(filter.Sevenths, facets.SeventhToken);
    }

    // A null level is unconstrained (matches all); otherwise the token must be in the enabled set (case-insensitive).
    private static bool LevelMatches(IReadOnlyList<string>? selected, string token) =>
        selected is null || selected.Contains(token, StringComparer.OrdinalIgnoreCase);

    // Realize one combo at the root via the shared derivation→diagram path. A combo with no clean grip at this
    // root/region (the derivation's region filter) is simply omitted — a partial grid, never an error (C5).
    private static FretboardDiagram? TryRealize(
        VoicingFamily family, Quality quality, CagedShape shape, PitchClass root, Key key)
    {
        try
        {
            ChordShape derived = FamilyVoicing.Derive(family, quality, shape, root, 0, NeckMaxFret);
            Voicing voicing = ChordShapeVoicing.ToVoicing(derived);
            return RealizedVoicingDiagram.Build(new Chord(root, quality), voicing, key);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}

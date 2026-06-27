using ChordFlow.Features.ContentCrud;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// The engine-derived <c>automatic</c> voicing source (engine-derived-as-app-source, req IN2; shell-voicing-
/// derivation IN8) — the implementation that fills the content-source-model union seam
/// (<see cref="IComputedContentSource"/>). It lists the pinned (family, quality, CAGED-shape) families
/// (<see cref="CagedVoicingCatalog"/>) as <c>automatic</c>-tagged catalog rows so they appear on the Content page
/// alongside the package and user voicings. These are root-independent catalog entries; they carry no grip
/// geometry — the comping resolver derives actual grips at render time. Computed, never stored (C3).
/// </summary>
public sealed class EngineVoicingSource : IComputedContentSource
{
    private static readonly IReadOnlyDictionary<Quality, string> DisplayNames = new Dictionary<Quality, string>
    {
        [Quality.Major] = "Major",
        [Quality.Minor] = "Minor",
        [Quality.Major7] = "Major 7",
        [Quality.Dominant7] = "Dominant 7",
        [Quality.Minor7] = "Minor 7",
        [Quality.HalfDiminished7] = "Half-diminished 7 (m7♭5)",
        [Quality.Diminished7] = "Diminished 7",
        [Quality.Augmented] = "Augmented",
        [Quality.Major6] = "Major 6",
        [Quality.Minor6] = "Minor 6",
    };

    private static readonly IReadOnlyDictionary<VoicingFamily, string> FamilySuffixes = new Dictionary<VoicingFamily, string>
    {
        [VoicingFamily.Caged] = "",
        [VoicingFamily.DoubledShell] = " (doubled shell)",
        [VoicingFamily.Shell] = " (shell)",
    };

    /// <summary>The <c>automatic</c> voicing rows for <see cref="ContentEntity.Voicing"/>; every other kind is empty.</summary>
    public IReadOnlyList<ContentItem> List(ContentEntity entity)
    {
        if (entity != ContentEntity.Voicing)
        {
            return Array.Empty<ContentItem>();
        }

        return CagedVoicingCatalog.Combos
            .Select(c => new ContentItem(
                AutomaticVoicingId.For(c.Family, c.Quality, c.Shape),
                DisplayName(c.Family, c.Quality, c.Shape),
                "automatic",
                PackName: null))
            .ToList();
    }

    /// <summary>The catalog display name for a family × quality × shape, e.g. "Dominant 7 (shell) — E shape".</summary>
    public static string DisplayName(VoicingFamily family, Quality quality, CagedShape shape) =>
        $"{DisplayNames[quality]}{FamilySuffixes[family]} — {shape} shape";

    /// <summary>The display name for an <c>auto:</c> id, or null if it is not one.</summary>
    public static string? DisplayNameFor(string id) =>
        AutomaticVoicingId.TryParse(id, out VoicingFamily family, out Quality quality, out CagedShape shape)
            ? DisplayName(family, quality, shape)
            : null;
}

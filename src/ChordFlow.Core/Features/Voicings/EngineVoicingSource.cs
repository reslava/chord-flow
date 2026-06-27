using ChordFlow.Features.ContentCrud;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// The engine-derived <c>automatic</c> voicing source (engine-derived-as-app-source, req IN2) — the
/// implementation that fills the content-source-model union seam (<see cref="IComputedContentSource"/>). It
/// lists the <b>36</b> pinned quality×CAGED-shape families (<see cref="CagedVoicingCatalog"/>) as
/// <c>automatic</c>-tagged catalog rows so they appear on the Content page alongside the package and user
/// voicings. These are root-independent catalog entries (like the canonical-C authored voicings); they carry
/// no grip geometry — the comping resolver derives actual grips at render time. Computed, never stored (C3):
/// it touches no <see cref="ChordFlow.Persistence.IContentStore"/> and never flows through SQLite.
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

    /// <summary>The 36 <c>automatic</c> voicing rows for <see cref="ContentEntity.Voicing"/>; every other kind is empty.</summary>
    public IReadOnlyList<ContentItem> List(ContentEntity entity)
    {
        if (entity != ContentEntity.Voicing)
        {
            return Array.Empty<ContentItem>();
        }

        return CagedVoicingCatalog.Combos
            .Select(c => new ContentItem(
                AutomaticVoicingId.For(c.Quality, c.Shape),
                DisplayName(c.Quality, c.Shape),
                "automatic",
                PackName: null))
            .ToList();
    }

    /// <summary>The catalog display name for a quality×shape family, e.g. "Dominant 7 — E shape".</summary>
    public static string DisplayName(Quality quality, CagedShape shape) => $"{DisplayNames[quality]} — {shape} shape";

    /// <summary>The display name for an <c>auto:</c> id, or null if it is not one.</summary>
    public static string? DisplayNameFor(string id) =>
        AutomaticVoicingId.TryParse(id, out Quality quality, out CagedShape shape) ? DisplayName(quality, shape) : null;
}

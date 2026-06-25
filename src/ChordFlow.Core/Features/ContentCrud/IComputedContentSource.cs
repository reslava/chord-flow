namespace ChordFlow.Features.ContentCrud;

/// <summary>
/// The <b>union seam</b> for the multi-source content model (content-source-model, req IN8): a non-store
/// content source whose items are <i>computed</i>, not persisted, and unioned into a kind's list alongside
/// the package + user rows from the <see cref="ChordFlow.Persistence.IContentStore"/>. It yields
/// <see cref="ContentItem"/>s already tagged <c>source = "automatic"</c>.
///
/// <para>This thread only defines the seam; no implementation is wired yet (the list contributes no
/// <c>automatic</c> rows). The engine-derived voicing source (engine-derived-as-app-source thread)
/// implements it for <see cref="ContentEntity.Voicing"/>.</para>
/// </summary>
public interface IComputedContentSource
{
    /// <summary>The computed items for <paramref name="entity"/> (empty when this source has none for that kind).</summary>
    IReadOnlyList<ContentItem> List(ContentEntity entity);
}

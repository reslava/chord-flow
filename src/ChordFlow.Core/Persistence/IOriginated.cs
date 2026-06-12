namespace ChordFlow.Persistence;

/// <summary>
/// A content definition that carries provenance — its stable <see cref="Id"/> and its <see cref="Origin"/>.
/// Implemented by every content entity so the shared <see cref="OriginResolver"/> can shadow-resolve across
/// tiers regardless of the concrete entity type (constraint C1: an Entity-layer concern).
/// </summary>
public interface IOriginated
{
    /// <summary>Stable definition id — the key shadowing resolves on.</summary>
    string Id { get; }

    /// <summary>Provenance tier of this copy.</summary>
    Origin Origin { get; }
}

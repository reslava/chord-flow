namespace ChordFlow.Features.ContentCrud;

/// <summary>
/// The four DSL-backed content entities the generic CRUD surface edits. One discriminator carried on every
/// <c>entity*</c> bridge envelope (design §4: one generic family, not per-entity envelopes); the
/// <see cref="ContentCrudHandler"/> maps it to the matching <c>IContentStore</c>.
/// </summary>
public enum ContentEntity
{
    Progression,
    Song,
    Rhythm,
    Voicing,
    Drums,
}

/// <summary>Parse the wire string (<c>progression|song|rhythm|voicing</c>) on an inbound envelope.</summary>
public static class ContentEntities
{
    public static ContentEntity Parse(string? entity) => entity?.Trim().ToLowerInvariant() switch
    {
        "progression" => ContentEntity.Progression,
        "song" => ContentEntity.Song,
        "rhythm" => ContentEntity.Rhythm,
        "voicing" => ContentEntity.Voicing,
        "drums" => ContentEntity.Drums,
        _ => throw new FormatException($"Unknown content entity \"{entity}\"."),
    };
}

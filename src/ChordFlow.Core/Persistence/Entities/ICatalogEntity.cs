namespace ChordFlow.Persistence.Entities;

/// <summary>
/// A content entity that carries the full catalog shape — provenance (<see cref="IOriginated"/>) plus the
/// shared mutable fields the pack importer upserts (<see cref="Name"/>, <see cref="Dsl"/>, <see cref="PackId"/>
/// and the denormalized <see cref="Genre"/>/<see cref="Subgenre"/>/<see cref="Tags"/> columns). Implemented by
/// <c>ProgressionEntity</c>, <c>SongEntity</c> and <c>VoicingEntity</c> so one generic upsert serves all three
/// (rhythm patterns carry no catalog metadata — EX3 — and are upserted separately).
/// </summary>
public interface ICatalogEntity : IOriginated
{
    string Name { get; set; }
    string Dsl { get; set; }
    string? PackId { get; set; }
    string? Genre { get; set; }
    string? Subgenre { get; set; }
    string Tags { get; set; }
}

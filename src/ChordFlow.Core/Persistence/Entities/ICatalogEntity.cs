namespace ChordFlow.Persistence.Entities;

/// <summary>
/// A content entity that carries the full catalog shape — provenance (<see cref="IOriginated"/>) plus the
/// shared mutable fields the pack importer upserts (<see cref="Name"/>, <see cref="Dsl"/>, <see cref="PackId"/>
/// and the denormalized <see cref="Genre"/>/<see cref="Subgenre"/>/<see cref="Tags"/> columns). Implemented by
/// <c>ProgressionEntity</c>, <c>SongEntity</c>, <c>VoicingEntity</c> and <c>DrumGrooveEntity</c> so one generic
/// upsert serves all four (rhythm patterns carry no catalog metadata — EX3 — and are upserted separately). The
/// denormalized columns are populated on every write (pack import + user saves) and reconciled from the
/// canonical DSL header by the startup <c>CatalogColumnBackfill</c>; <c>List()</c> now reads them directly
/// (content-list-reads-columns), while the header remains the canonical source of catalog metadata.
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

namespace ChordFlow.Persistence.Entities;

/// <summary>
/// A content entity that carries the full catalog shape — provenance (<see cref="IOriginated"/>) plus the
/// shared mutable fields the pack importer upserts (<see cref="Name"/>, <see cref="Dsl"/>, <see cref="PackId"/>
/// and the denormalized <see cref="Genre"/>/<see cref="Subgenre"/>/<see cref="Tags"/> columns). Implemented by
/// <c>ProgressionEntity</c>, <c>SongEntity</c>, <c>VoicingEntity</c> and <c>DrumGrooveEntity</c> so one generic
/// upsert serves all four (rhythm patterns carry no catalog metadata — EX3 — and are upserted separately). The
/// denormalized columns are now populated on user saves too (content-metadata-editing), though <c>List()</c>
/// still reads the DSL header (the canonical source); switching the read path to the columns is deferred.
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

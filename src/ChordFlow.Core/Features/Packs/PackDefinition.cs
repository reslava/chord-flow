namespace ChordFlow.Features.Packs;

/// <summary>
/// One definition read from a pack <c>.dsl</c> file. <see cref="Kind"/> comes from the folder it sat in;
/// <see cref="Id"/> is the filename stem (design §6.4: filename = id); <see cref="Name"/> is the optional
/// leading <c>name:</c> header line (falling back to a title-cased id); <see cref="Dsl"/> is the file text
/// with the <c>name:</c> line removed — the catalog header (<c>genre</c>/<c>subgenre</c>/<c>tags</c>), if
/// present, stays, so the importer denormalizes it into entity columns exactly as first-run seeding does.
/// </summary>
public sealed record PackDefinition(ContentKind Kind, string Id, string Name, string Dsl);

/// <summary>
/// A loaded pack: its <see cref="Manifest"/> plus every <see cref="PackDefinition"/> across all kind
/// folders, in <see cref="ContentKinds.All"/> order. The in-memory result of <see cref="PackReader"/>;
/// the importer (step 5) upserts these by id.
/// </summary>
public sealed record ContentPack(PackManifest Manifest, IReadOnlyList<PackDefinition> Definitions);

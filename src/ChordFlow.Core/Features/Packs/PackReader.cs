namespace ChordFlow.Features.Packs;

/// <summary>
/// Loads a pack bundle from a directory (design §3): reads <c>manifest.json</c>, validates the pack
/// <see cref="PackManifest.Kind"/> is supported, then walks each per-kind folder (<c>progressions/</c>,
/// <c>songs/</c>, <c>rhythms/</c>, <c>voicings/</c>) reading every <c>*.dsl</c> file into a
/// <see cref="PackDefinition"/>. Each kind folder is optional — a pack carries any mix (mixed packs, C4).
/// This is the I/O seam; the parsing it composes (<see cref="PackManifest.Parse"/>,
/// <see cref="PackDefinitionFile.Read"/>) is pure. Importing the result is a separate step.
/// </summary>
public static class PackReader
{
    /// <summary>The manifest file every pack must contain.</summary>
    public const string ManifestFileName = "manifest.json";

    /// <summary>
    /// Read the pack rooted at <paramref name="directory"/> into a <see cref="ContentPack"/>. Throws
    /// <see cref="DirectoryNotFoundException"/> if the directory is missing, <see cref="FileNotFoundException"/>
    /// if it has no <c>manifest.json</c>, <see cref="FormatException"/> on a malformed manifest, and
    /// <see cref="NotSupportedException"/> if the manifest declares a pack kind this build cannot import.
    /// </summary>
    public static ContentPack ReadFromDirectory(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"pack directory not found: {directory}");
        }

        string manifestPath = Path.Combine(directory, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"pack is missing {ManifestFileName}.", manifestPath);
        }

        PackManifest manifest = PackManifest.Parse(File.ReadAllText(manifestPath));
        if (!string.Equals(manifest.Kind, PackManifest.ContentKindLabel, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"pack '{manifest.Id}' has kind '{manifest.Kind}'; only '{PackManifest.ContentKindLabel}' packs are supported today.");
        }

        var definitions = new List<PackDefinition>();
        foreach (ContentKind kind in ContentKinds.All)
        {
            string folder = Path.Combine(directory, kind.Folder());
            if (!Directory.Exists(folder))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(folder, "*.dsl").OrderBy(f => f, StringComparer.Ordinal))
            {
                definitions.Add(PackDefinitionFile.Read(kind, Path.GetFileName(file), File.ReadAllText(file)));
            }
        }

        return new ContentPack(manifest, definitions);
    }
}

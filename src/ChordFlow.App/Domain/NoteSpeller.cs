namespace ChordFlow.Domain;

/// <summary>
/// Resolves a <see cref="PitchClass"/> to a correctly-spelled note name for a <see cref="Key"/>
/// (sharp vs flat per key) — spelling is derived, never stored on the pitch class (constraint C4).
/// PC 1 spells <c>C#</c> in D major but <c>Db</c> in Ab major. Promoted out of the renderer's
/// hardcoded key arrays so the domain owns spelling and the renderer just formats.
/// </summary>
public static class NoteSpeller
{
    private static readonly string[] Sharp =
        { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    private static readonly string[] Flat =
        { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

    // Whether each major key (indexed by tonic pitch class) is conventionally spelled with sharps.
    // Matches the renderer's previous MajorKeyName/MajorKeySignature choices: pc1 = Db (flat),
    // pc6 = F# (sharp), pc8 = Ab (flat), etc. C (pc0) has no accidentals — defaults to flats.
    private static readonly bool[] MajorUsesSharps =
    {
        false, // C
        false, // Db
        true,  // D
        false, // Eb
        true,  // E
        false, // F
        true,  // F#
        true,  // G
        false, // Ab
        true,  // A
        false, // Bb
        true,  // B
    };

    /// <summary>The note name of <paramref name="pitchClass"/> spelled for <paramref name="key"/>.</summary>
    public static string Name(PitchClass pitchClass, Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        string[] table = UsesSharps(key) ? Sharp : Flat;
        return table[Mod12(pitchClass.Value)];
    }

    /// <summary>
    /// The alphaTex <c>\ks</c> key-signature token for <paramref name="key"/> — its tonic spelled
    /// and lowercased (e.g. <c>bb</c>, <c>f#</c>).
    /// </summary>
    public static string KeySignatureToken(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Name(key.Tonic, key).ToLowerInvariant();
    }

    // A key spells with sharps based on its (relative) major. The relative major of a minor key is
    // a minor third (3 semitones) up, so Am → C, Em → G, etc.
    private static bool UsesSharps(Key key)
    {
        int majorTonic = Mod12(key.IsMinor ? key.Tonic.Value + 3 : key.Tonic.Value);
        return MajorUsesSharps[majorTonic];
    }

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}

namespace ChordFlow.Music.Harmony;

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
    /// and lowercased (e.g. <c>bb</c>, <c>f#</c>). A minor key appends the <c>minor</c> mode suffix
    /// (<c>aminor</c>, <c>c#minor</c>), which alphaTab accepts natively (<c>\ks Aminor</c>); a major
    /// key stays a bare note so existing output is byte-identical (first-class-minor-keys, IN3).
    /// </summary>
    public static string KeySignatureToken(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        string tonic = Name(key.Tonic, key).ToLowerInvariant();
        return key.IsMinor ? tonic + "minor" : tonic;
    }

    /// <summary>
    /// Inverse of <see cref="KeySignatureToken"/>: parse a <c>\ks</c>-style token (e.g. <c>bb</c>,
    /// <c>f#</c>, <c>c</c>, or a minor <c>aminor</c> / <c>c#minor</c>) back into a <see cref="Key"/>.
    /// Used to round-trip a persisted <c>Exercise.KeyOverride</c>; a trailing <c>minor</c> suffix sets
    /// <see cref="Key.IsMinor"/> (first-class-minor-keys, IN3).
    /// </summary>
    public static Key KeyFromSignatureToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        bool isMinor = token.EndsWith("minor", StringComparison.OrdinalIgnoreCase);
        string note = isMinor ? token[..^"minor".Length] : token;
        if (note.Length == 0)
        {
            throw new FormatException($"Key token \"{token}\" has no note letter.");
        }

        int basePc = char.ToUpperInvariant(note[0]) switch
        {
            'C' => 0, 'D' => 2, 'E' => 4, 'F' => 5, 'G' => 7, 'A' => 9, 'B' => 11,
            _ => throw new FormatException($"Key token \"{token}\" has an unknown note letter."),
        };

        if (note.Length > 1)
        {
            basePc += note[1] switch
            {
                '#' => 1,
                'b' => -1,
                _ => throw new FormatException($"Key token \"{token}\" has an unknown accidental \"{note[1]}\"."),
            };
        }

        return new Key(new PitchClass(Mod12(basePc)), IsMinor: isMinor);
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

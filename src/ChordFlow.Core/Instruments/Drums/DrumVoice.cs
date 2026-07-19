namespace ChordFlow.Instruments.Drums;

/// <summary>
/// A percussion voice — one lane of a <see cref="DrumGroove"/>. Each voice maps to the alphaTex
/// articulation token our vendored <b>alphaTab 1.8.3</b> registers under <c>\articulation defaults</c>
/// (the token the percussion renderer emits). alphaTab keys each default articulation by
/// <c>toArticulationId(name) = name.replace(/[^a-zA-Z0-9]/g, "").toLowerCase()</c>, so the token is the
/// display name stripped to lowercase alphanumerics — e.g. <c>"Hi-Hat (closed)"</c> → <c>hihatclosed</c>,
/// <c>"Pedal Hi-Hat (hit)"</c> → <c>pedalhihathit</c>. (The idea's <c>KickHit</c>-style names were alphaTab
/// 2.x/next; 1.8.3 uses these.) The vocabulary is instrument-specific, so it lives under
/// <c>Instruments/Drums/</c>, never in the agnostic <c>Music/</c> kernel (req C1). Articulation variety is
/// expressed as distinct voices/lanes — open vs closed hi-hat are two voices — not glyph variants (req C3).
/// </summary>
public enum DrumVoice
{
    /// <summary>Bass/kick drum — alphaTex <c>kickhit</c> (GM 35/36).</summary>
    Kick,

    /// <summary>Snare drum — alphaTex <c>snarehit</c> (GM 38).</summary>
    Snare,

    /// <summary>Closed hi-hat — alphaTex <c>hihatclosed</c> (GM 42).</summary>
    HiHatClosed,

    /// <summary>Open hi-hat — alphaTex <c>hihatopen</c> (GM 46).</summary>
    HiHatOpen,

    /// <summary>Foot/pedal hi-hat — alphaTex <c>pedalhihathit</c> (GM 44).</summary>
    HiHatPedal,

    /// <summary>Ride cymbal — alphaTex <c>ridemiddle</c> (GM 51).</summary>
    Ride,

    /// <summary>Ride bell — alphaTex <c>ridebell</c> (GM 53).</summary>
    RideBell,

    /// <summary>Crash cymbal — alphaTex <c>crashhighhit</c> (GM 49).</summary>
    Crash,

    /// <summary>High tom — alphaTex <c>hightomhit</c> (GM 48).</summary>
    HighTom,

    /// <summary>Mid tom — alphaTex <c>midtomhit</c> (GM 47).</summary>
    MidTom,

    /// <summary>Floor tom — alphaTex <c>lowfloortomhit</c> (GM 41).</summary>
    FloorTom,
}

/// <summary>
/// The single source of the <see cref="DrumVoice"/> vocabulary: the alphaTab 1.8.3 articulation token each
/// voice renders to, the canonical short token used in the hit-grid DSL and stored form, and the alias set
/// the parser accepts (short token + full names, case-insensitive). Kept here so the DSL parser, the
/// renderer, and DrumsR all share one table and cannot drift (the peer of <c>VoicingFamilies</c>).
/// </summary>
public static class DrumVoices
{
    // alphaTab 1.8.3 \articulation defaults tokens (toArticulationId form — lowercase, alphanumeric-only, so
    // they tokenize as bare alphaTex note idents). Verified against the vendored engine's default kit table.
    private static readonly IReadOnlyDictionary<DrumVoice, string> Articulations = new Dictionary<DrumVoice, string>
    {
        [DrumVoice.Kick] = "kickhit",
        [DrumVoice.Snare] = "snarehit",
        [DrumVoice.HiHatClosed] = "hihatclosed",
        [DrumVoice.HiHatOpen] = "hihatopen",
        [DrumVoice.HiHatPedal] = "pedalhihathit",
        [DrumVoice.Ride] = "ridemiddle",
        [DrumVoice.RideBell] = "ridebell",
        [DrumVoice.Crash] = "crashhighhit",
        [DrumVoice.HighTom] = "hightomhit",
        [DrumVoice.MidTom] = "midtomhit",
        [DrumVoice.FloorTom] = "lowfloortomhit",
    };

    // The canonical short token (first alias) each voice serializes to.
    private static readonly IReadOnlyDictionary<DrumVoice, string> CanonicalTokens = new Dictionary<DrumVoice, string>
    {
        [DrumVoice.Kick] = "BD",
        [DrumVoice.Snare] = "SD",
        [DrumVoice.HiHatClosed] = "HH",
        [DrumVoice.HiHatOpen] = "OH",
        [DrumVoice.HiHatPedal] = "PH",
        [DrumVoice.Ride] = "RD",
        [DrumVoice.RideBell] = "RB",
        [DrumVoice.Crash] = "CC",
        [DrumVoice.HighTom] = "HT",
        [DrumVoice.MidTom] = "MT",
        [DrumVoice.FloorTom] = "FT",
    };

    // token (canonical short + full-name aliases) → voice. Case-insensitive so authors can type BD/bd/Kick.
    private static readonly IReadOnlyDictionary<string, DrumVoice> ByToken =
        new Dictionary<string, DrumVoice>(StringComparer.OrdinalIgnoreCase)
        {
            ["BD"] = DrumVoice.Kick,
            ["Kick"] = DrumVoice.Kick,
            ["KD"] = DrumVoice.Kick,

            ["SD"] = DrumVoice.Snare,
            ["Snare"] = DrumVoice.Snare,

            ["HH"] = DrumVoice.HiHatClosed,
            ["HiHat"] = DrumVoice.HiHatClosed,
            ["CH"] = DrumVoice.HiHatClosed,

            ["OH"] = DrumVoice.HiHatOpen,
            ["OpenHat"] = DrumVoice.HiHatOpen,

            ["PH"] = DrumVoice.HiHatPedal,
            ["FootHat"] = DrumVoice.HiHatPedal,
            ["HF"] = DrumVoice.HiHatPedal,

            ["RD"] = DrumVoice.Ride,
            ["Ride"] = DrumVoice.Ride,

            ["RB"] = DrumVoice.RideBell,
            ["RideBell"] = DrumVoice.RideBell,

            ["CC"] = DrumVoice.Crash,
            ["Crash"] = DrumVoice.Crash,

            ["HT"] = DrumVoice.HighTom,
            ["HighTom"] = DrumVoice.HighTom,

            ["MT"] = DrumVoice.MidTom,
            ["MidTom"] = DrumVoice.MidTom,

            ["FT"] = DrumVoice.FloorTom,
            ["FloorTom"] = DrumVoice.FloorTom,
        };

    /// <summary>The alphaTab 1.8.3 articulation token for <paramref name="voice"/> (e.g. <c>kickhit</c>).</summary>
    public static string Articulation(this DrumVoice voice) => Articulations[voice];

    /// <summary>The canonical short DSL/stored token for <paramref name="voice"/> (e.g. <c>BD</c>).</summary>
    public static string Token(this DrumVoice voice) => CanonicalTokens[voice];

    /// <summary>
    /// Resolve a DSL row label (short token or full-name alias, case-insensitive) to its
    /// <see cref="DrumVoice"/>; false when <paramref name="token"/> is not a known voice.
    /// </summary>
    public static bool TryParse(string token, out DrumVoice voice) => ByToken.TryGetValue(token, out voice);
}

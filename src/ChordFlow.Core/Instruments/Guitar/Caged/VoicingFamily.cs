namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// A derived <c>automatic</c> voicing family — the product applied to a quality × CAGED placement
/// (shell-voicing-derivation, req IN2):
/// <list type="bullet">
/// <item><see cref="Caged"/> — the full derived chord (<c>CagedDerivation</c>).</item>
/// <item><see cref="DoubledShell"/> — that chord with the 5th muted, doublings kept (<c>ShellReduction</c>).</item>
/// <item><see cref="Shell"/> — the compact 3-note guide-tone shell, root + 3rd + 7th|6th, a distinct 2-form
///   derivation (<c>ShellDerivation</c>).</item>
/// </list>
/// </summary>
public enum VoicingFamily
{
    Caged,
    DoubledShell,
    Shell,
}

/// <summary>
/// Token mapping for <see cref="VoicingFamily"/> — the segment used in synthetic ids
/// (<c>caged</c>/<c>dshell</c>/<c>shell</c>, e.g. <c>auto:shell:dom7:E</c>). The vocabulary lives here so the
/// id scheme and any parser share one source.
/// </summary>
public static class VoicingFamilies
{
    private static readonly IReadOnlyDictionary<VoicingFamily, string> Tokens = new Dictionary<VoicingFamily, string>
    {
        [VoicingFamily.Caged] = "caged",
        [VoicingFamily.DoubledShell] = "dshell",
        [VoicingFamily.Shell] = "shell",
    };

    private static readonly IReadOnlyDictionary<string, VoicingFamily> ByToken =
        Tokens.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    /// <summary>The token (e.g. <c>dshell</c>) of <paramref name="family"/>.</summary>
    public static string Token(this VoicingFamily family) => Tokens[family];

    /// <summary>Parse a family token back to its <see cref="VoicingFamily"/>; false if it is not a known token.</summary>
    public static bool TryParse(string token, out VoicingFamily family) => ByToken.TryGetValue(token, out family);
}

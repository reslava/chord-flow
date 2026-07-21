namespace ChordFlow.Music.Rhythm.Generation;

/// <summary>
/// The curated **named groove figure** catalog (design §3a v2) — musically useful bar patterns, each a
/// singleton (or multi-bar, for claves) <see cref="RhythmKind"/> that doubles as a preset. Cheap **data**:
/// adding a figure is a line here, no engine change. Cell masks (`x` = onset) are authored best-effort and
/// verified by ear in the app. Multi-bar figures (claves) are played by choosing the <c>Cycle</c> selection,
/// which tours their bars in order.
/// </summary>
public static class GrooveFigures
{
    private static RhythmKind Fig(string id, string name, int subdivision, params string[] barMasks) =>
        new(id, name, "figure", barMasks.Select(m => OnsetBar.FromMask(subdivision, m)).ToArray());

    /// <summary>The figures, in offered order (quarter-grid first, then eighth, then 2-bar claves).</summary>
    public static readonly IReadOnlyList<RhythmKind> All = new[]
    {
        Fig("four-on-floor", "Four-on-the-floor", 1, "xxxx"),
        Fig("downbeats", "Downbeats (1 & 3)", 1, "x.x."),
        Fig("backbeat", "Backbeat (2 & 4)", 1, ".x.x"),
        Fig("beat1", "Beat-1 anchor", 1, "x..."),
        Fig("straight-8ths", "Straight eighths", 2, "xxxxxxxx"),
        Fig("offbeats", "Offbeats (all &s)", 2, ".x.x.x.x"),
        Fig("charleston", "Charleston", 2, "x..x...."),
        Fig("rev-charleston", "Reverse Charleston", 2, "....x..x"),
        Fig("tresillo", "Tresillo (3-3-2)", 2, "x..x..x."),
        Fig("cinquillo", "Cinquillo", 2, "x.xx.xx."),
        Fig("dotted-push", "Dotted-quarter push", 2, "x..x..xx"),
        Fig("habanera", "Habanera", 2, "x..xx.x."),
        Fig("son-clave-32", "Son clave (3-2)", 2, "x..x..x.", "..x.x..."),
        Fig("son-clave-23", "Son clave (2-3)", 2, "..x.x...", "x..x..x."),
        Fig("rumba-clave-32", "Rumba clave (3-2)", 2, "x..x...x", "..x.x..."),
        Fig("bossa-clave", "Bossa clave", 2, "x..x..x.", "..x.x..x"),
    };

    /// <summary>Resolve a figure by id; null when unknown.</summary>
    public static RhythmKind? ById(string id) => All.FirstOrDefault(k => k.Id == id);
}

using ChordFlow.Instruments.Guitar;
using ChordFlow.Music.Harmony;

namespace ChordFlow.Features.Voicings;

/// <summary>
/// Resolves an <c>automatic</c> voicing family id (<c>auto:shell:dom7:E</c> …) to a canonical voicing DSL line
/// (engine-derived-as-app-source IN13): derive the family's <b>lowest valid grip at canonical C</b> and write
/// it via the normal <see cref="VoicingDslWriter"/>. This lets the Content view show a computed (un-stored)
/// voicing through the existing read-only preview / "Duplicate to user" path — no special-case rendering.
/// <para>Robust to the <see cref="CagedDerivation"/> extreme-placement throw (caged-derive-anchor-edge): it
/// scans upward for the lowest fret window that derives cleanly, rather than failing at the full neck.</para>
/// </summary>
public static class AutomaticVoicingDoc
{
    private static readonly PitchClass C = new(0);

    /// <summary>
    /// The canonical voicing DSL for <paramref name="id"/> if it is an <c>auto:</c> family id, else
    /// <c>null</c>. Throws <see cref="InvalidOperationException"/> when no placement in <c>[0,15]</c> derives.
    /// </summary>
    public static string? DslFor(string id)
    {
        if (!AutomaticVoicingId.TryParse(id, out VoicingFamily family, out Quality quality, out CagedShape shape))
        {
            return null;
        }

        ChordShape grip = LowestValidPlacement(family, quality, shape);
        int rootString = OctaveShape.RootStrings(shape).Max(); // the bass root string for this CAGED shape / form
        var voicingShape = new VoicingShape(quality, shape, rootString, ChordShapeVoicing.ToVoicing(grip));
        return VoicingDslWriter.ToDsl(voicingShape); // canonical-C; the parser re-normalizes on read
    }

    // The lowest fret window whose grip derives without a throw — the resolver's region filter for one shape.
    private static ChordShape LowestValidPlacement(VoicingFamily family, Quality quality, CagedShape shape)
    {
        for (int minFret = 0; minFret <= 12; minFret++)
        {
            try
            {
                return FamilyVoicing.Derive(family, quality, shape, C, minFret, 15);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
            {
                // This window has no clean grip for the shape — try a higher anchor.
            }
        }

        throw new InvalidOperationException($"No derivable placement for {quality} {shape} in [0,15].");
    }
}

using System.Collections.Generic;
using System.Linq;
using ChordFlow.Instruments.Drums;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
namespace ChordFlow.Exercises;

/// <summary>
/// The composed practice unit the engine plays — the one canonical play-unit (merge decision (a),
/// <c>exercises-definition-ui-chat-002</c>). Definition is a <see cref="Song"/> (harmony + arrangement; a bare
/// progression is lifted via <see cref="Song.OfProgression"/>) plus a typed list of <see cref="InstrumentPart"/>s
/// (<c>drums-under-a-song</c> D1/IN1) — the parts union that <b>replaced</b> the old flat
/// <c>Comping</c>/<c>Lead</c> fields, so a new instrument (drums now, bass later) is an additive union arm rather
/// than another play-unit remodel. Params are values with saved defaults: <see cref="KeyOverride"/> (null →
/// <see cref="Song.InitialKey"/>; else a global transpose), <see cref="Tempo"/>, <see cref="Difficulty"/>, and
/// groove <see cref="TripletFeel"/>.
/// <para>
/// Realization is the single path <see cref="SongExpander.Expand"/> → <c>RealizedSong</c> →
/// <c>AlphaTexRenderer.Render(RealizedSong, …)</c>; the expansion (the one I/O seam, needs the
/// <see cref="IProgressionStore"/>) lives in the Features layer, never the renderer (decision (a)). The renderer
/// is handed the <b>extracted typed pieces</b> (via <see cref="Comping"/>/<see cref="Lead"/>/<see cref="Drums"/>),
/// never the <see cref="Parts"/> union. Pure/immutable, no I/O (C3). <see cref="TripletFeel"/> is a play-time
/// choice delegated to alphaTab's <c>\tf</c> directive at render, never baked into the pattern.
/// </para>
/// <para>
/// <b>Invariants</b> (fail-loud, enforced by the accessors — req C4): exactly one <see cref="CompingPart"/>;
/// at most one <see cref="LeadPart"/> and one <see cref="DrumPart"/>. Per-part mix (volume/mute) rides the part;
/// the shared key/tempo/feel/difficulty stay here.
/// </para>
/// </summary>
public sealed record Exercise(
    // ── Definition ──
    Song Song,
    IReadOnlyList<InstrumentPart> Parts,

    // ── Params (values — saved defaults, user-editable at play) ──
    Key? KeyOverride,
    int Tempo,
    Difficulty Difficulty,
    TripletFeel TripletFeel = TripletFeel.None)
{
    /// <summary>
    /// Convenience constructor for the common guitar case — a required comping pattern + optional lead, no drums.
    /// Delegates to the canonical <see cref="Parts"/> shape so the pre-parts callers stay unchanged; a drum part
    /// is added by building <see cref="Parts"/> directly (the generate/load paths).
    /// </summary>
    public Exercise(
        Song Song, RhythmPattern Comping, RhythmPattern? Lead, Key? KeyOverride, int Tempo,
        Difficulty Difficulty, TripletFeel TripletFeel = TripletFeel.None)
        : this(Song, BuildGuitarParts(Comping, Lead), KeyOverride, Tempo, Difficulty, TripletFeel)
    {
    }

    /// <summary>The required rhythm-guitar comping pattern (exactly one — throws if absent or ambiguous, C4).</summary>
    public RhythmPattern Comping => Parts.OfType<CompingPart>().Single().Pattern;

    /// <summary>The optional lead pattern (at most one; null if none).</summary>
    public RhythmPattern? Lead => Parts.OfType<LeadPart>().SingleOrDefault()?.Pattern;

    /// <summary>The optional drum groove tiled beneath the harmony (at most one; null if none).</summary>
    public DrumGroove? Drums => Parts.OfType<DrumPart>().SingleOrDefault()?.Groove;

    private static IReadOnlyList<InstrumentPart> BuildGuitarParts(RhythmPattern comping, RhythmPattern? lead)
    {
        var parts = new List<InstrumentPart> { new CompingPart(comping) };
        if (lead is not null)
        {
            parts.Add(new LeadPart(lead));
        }

        return parts;
    }
}

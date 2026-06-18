namespace ChordFlow.Domain;

/// <summary>
/// Derives lead-training <see cref="TargetZone"/>s for a chord as pitch classes (ctx IN14) — pure and
/// instrument-agnostic. Guide tones (the 3rd &amp; 7th) come straight from the chord's interval set — no
/// per-chord authoring — so e.g. a ii–V–I produces its guide-tone line automatically. Resolving a zone to
/// concrete guitar frets is an instrument concern and lives on <c>GuitarInstrument.ResolveLead</c>
/// (Instruments/Guitar), keeping <c>Domain</c> free of any fretboard reference.
/// </summary>
public static class LeadTargets
{
    /// <summary>
    /// The guide-tone <see cref="TargetZone"/>s of <paramref name="chord"/>: its 3rd and (if present)
    /// 7th, both <see cref="Importance.Primary"/>.
    /// </summary>
    public static IReadOnlyList<TargetZone> GuideTones(Chord chord)
    {
        ArgumentNullException.ThrowIfNull(chord);

        return ChordTones.Of(chord)
            .Where(t => t.Function is ChordToneFunction.Third or ChordToneFunction.Seventh)
            .Select(t => new TargetZone(t, Importance.Primary))
            .ToArray();
    }

    /// <summary>The concrete pitch class of <paramref name="zone"/> for <paramref name="chord"/>.</summary>
    public static PitchClass PitchClassOf(Chord chord, TargetZone zone)
    {
        ArgumentNullException.ThrowIfNull(chord);
        return zone.Tone.PitchClassFor(chord.Root);
    }
}

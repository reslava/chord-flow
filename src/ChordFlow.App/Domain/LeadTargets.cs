namespace ChordFlow.Domain;

/// <summary>
/// Derives lead-training <see cref="TargetZone"/>s for a chord and resolves them to the fretboard
/// (ctx IN14). Guide tones (the 3rd &amp; 7th) come straight from the chord's interval set — no
/// per-chord authoring — so e.g. a ii–V–I produces its guide-tone line automatically.
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

    /// <summary>
    /// Every fretboard <see cref="FretPosition"/> (0..<paramref name="maxFret"/>) that sounds
    /// <paramref name="zone"/> over <paramref name="chord"/>.
    /// </summary>
    public static IReadOnlyList<FretPosition> Resolve(Chord chord, TargetZone zone, int maxFret = Fretboard.DefaultMaxFret) =>
        Fretboard.PositionsFor(PitchClassOf(chord, zone), maxFret);
}

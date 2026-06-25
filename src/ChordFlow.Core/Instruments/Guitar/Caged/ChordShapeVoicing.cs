namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// Adapts an engine-derived <see cref="ChordShape"/> (the output of <see cref="CagedDerivation.Derive"/>)
/// into a <see cref="Voicing"/> for rendering and ranking (engine-derived-as-app-source, req IN1). The
/// mapping is lossless for everything a <see cref="Voicing"/> needs: each sounded
/// <see cref="ChordShapeString"/> becomes a <see cref="FretPosition"/>, muted strings become
/// <see cref="Voicing.MutedStrings"/>, and <see cref="Voicing.FirstFret"/> is the lowest sounding fret.
/// <para>
/// <see cref="Voicing.BarreFret"/> is left <c>null</c> — the derivation engine does not model a barre (req
/// EX5); the grip still renders and plays correctly, only the diagram's barre arc is absent. Deriving a barre
/// from the anchor finger + repeated frets is a later refinement.
/// </para>
/// </summary>
public static class ChordShapeVoicing
{
    /// <summary>The <see cref="Voicing"/> equivalent of the derived <paramref name="shape"/>.</summary>
    public static Voicing ToVoicing(ChordShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        var positions = new List<FretPosition>();
        var muted = new List<int>();
        foreach (ChordShapeString s in shape.Strings)
        {
            if (s.IsMuted)
            {
                muted.Add(s.String);
            }
            else
            {
                positions.Add(new FretPosition(s.String, s.Fret!.Value));
            }
        }

        int? firstFret = positions.Count > 0 ? positions.Min(p => p.Fret) : null;
        return new Voicing(
            positions,
            BarreFret: null,
            FirstFret: firstFret,
            MutedStrings: muted.Count > 0 ? muted : null);
    }
}

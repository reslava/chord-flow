namespace ChordFlow.Domain;

/// <summary>
/// Chord quality. The v1 set is backed by interval tables in <see cref="QualityIntervals"/>
/// (see ctx constraint C5) — chord tones, guide tones and lead targets all derive from those
/// intervals rather than being hand-authored per chord.
/// </summary>
public enum Quality
{
    /// <summary>Major triad — {0, 4, 7}.</summary>
    Major,

    /// <summary>Minor triad — {0, 3, 7}.</summary>
    Minor,

    /// <summary>Dominant 7th — {0, 4, 7, 10}.</summary>
    Dominant7,

    /// <summary>Major 7th — {0, 4, 7, 11}.</summary>
    Major7,

    /// <summary>Minor 7th — {0, 3, 7, 10}.</summary>
    Minor7,

    /// <summary>Half-diminished / minor 7 flat 5 — {0, 3, 6, 10}.</summary>
    HalfDiminished7,

    /// <summary>Diminished triad — {0, 3, 6}.</summary>
    Diminished,

    /// <summary>Diminished 7th — {0, 3, 6, 9}; the fully-symmetric stack of minor 3rds (1 b3 b5 bb7).</summary>
    Diminished7,

    /// <summary>Augmented triad — {0, 4, 8}.</summary>
    Augmented,
}

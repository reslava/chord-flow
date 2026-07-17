using ChordFlow.Music.Harmony;
namespace ChordFlow.Music.Progressions;

/// <summary>
/// The tonality a progression's DSL is authored in (first-class-minor-keys, C frame). v1 ships
/// <see cref="Major"/> + <see cref="Minor"/>; the five other diatonic modes are the growth path — each is
/// just a different parent-major rotation in <see cref="DegreeFrameConverter"/>, no new kernel scale.
/// </summary>
public enum Tonality
{
    Major,
    Minor,
}

/// <summary>
/// The DSL-frame converter (first-class-minor-keys, decision C). Everything the kernel realizes is stored in
/// one absolute <b>parent-major</b> frame (the major key sharing the mode's key signature); a progression's
/// <see cref="Tonality"/> is only an authoring lens applied at the DSL edges. This pure, stateless converter
/// is that lens: it rotates an author-frame degree to the parent-major degree at parse time
/// (<see cref="ToParent"/>) and back for display (<see cref="ToAuthor"/>), and resolves a key to the tonic of
/// its parent major (<see cref="ParentTonic"/>) for realization. No per-mode scale table ever enters the
/// kernel (constraint C2).
/// </summary>
public static class DegreeFrameConverter
{
    /// <summary>
    /// The tonic of a key's <b>parent major</b> — the major key sharing its signature. Major: the tonic
    /// itself (identity). Minor: the relative major, a minor third (3 semitones) up (A minor → C).
    /// </summary>
    public static PitchClass ParentTonic(Key key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new PitchClass(Mod12(key.Tonic.Value + (key.IsMinor ? 3 : 0)));
    }

    /// <summary>
    /// Author-frame degree → parent-major degree, used at parse. The mode's tonic sits on a fixed degree of
    /// its parent major (minor's tonic = the parent's 6th), so a minor degree rotates
    /// <c>1→6 2→7 3→1 4→2 5→3 6→4 7→5</c>. The accidental carries through <b>unchanged</b> — the author-degree
    /// and its parent-degree are the same physical scale note, so a <c>#</c>/<c>b</c> raises/lowers it identically.
    /// </summary>
    public static RomanDegree ToParent(RomanDegree degree, Tonality home) =>
        degree with { Degree = Rotate(degree.Degree, ParentPosition(home) - 1) };

    /// <summary>Inverse of <see cref="ToParent"/> — parent-major degree → author-frame degree, used for display.</summary>
    public static RomanDegree ToAuthor(RomanDegree degree, Tonality home) =>
        degree with { Degree = Rotate(degree.Degree, Degrees - (ParentPosition(home) - 1)) };

    private const int Degrees = 7;

    // The 1-based degree of the parent major on which the mode's tonic sits (Ionian/major → 1, Aeolian/minor → 6).
    private static int ParentPosition(Tonality home) => home == Tonality.Minor ? 6 : 1;

    // Rotate a 1-based degree forward by `by` steps within the 7-degree cycle.
    private static int Rotate(int degree, int by) => ((degree - 1 + by) % Degrees) + 1;

    private static int Mod12(int value) => ((value % 12) + 12) % 12;
}

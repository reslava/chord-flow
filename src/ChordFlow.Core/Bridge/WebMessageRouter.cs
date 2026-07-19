using ChordFlow.Exercises;
using ChordFlow.Music.Rhythm;
using System.Text.Json;
using ChordFlow.Rendering;

namespace ChordFlow.Bridge;

/// <summary>
/// The UI's chosen content references + params for one <c>generate</c> request — a stored Song or a bare
/// Progression for harmony (the <see cref="HarmonyEntity"/> discriminator mirrors the content-CRUD
/// <c>entity</c> string), a required Comping pattern, an optional Lead pattern, plus the param values. The
/// host resolves the references against the stores (<c>ExerciseRefs</c>) into a canonical Exercise.
/// </summary>
public sealed record GenerateRequest(
    string HarmonyEntity, string HarmonyId, string CompingPatternId, string? LeadPatternId,
    int? KeyPitchClass, int Tempo, Difficulty Difficulty, TripletFeel TripletFeel, bool KeyIsMinor = false,
    string? DrumGrooveId = null, double DrumVolume = 1.0);

/// <summary>
/// The faceted filter state for one <c>voicingGrid</c> request (GuitarVoicingsR): the chosen <see cref="Root"/>
/// (single global pitch class) plus the multi-select <b>enabled-token</b> sets for each level —
/// <see cref="Sources"/> (automatic/package/user), <see cref="Families"/> (caged/dshell/shell), and the
/// (3rd × 5th × 7th) facet axes <see cref="Thirds"/>/<see cref="Fifths"/>/<see cref="Sevenths"/>. A
/// <c>null</c> level is unconstrained (matches all); membership is OR within a level, AND across levels.
/// </summary>
public sealed record VoicingGridFilter(
    int Root,
    IReadOnlyList<string>? Sources,
    IReadOnlyList<string>? Families,
    IReadOnlyList<string>? Thirds,
    IReadOnlyList<string>? Fifths,
    IReadOnlyList<string>? Sevenths);

/// <summary>
/// One <c>voicingDerive</c> request from the Voicings Engine inspector page: the operator <see cref="Family"/>
/// (caged/dshell/shell), the <see cref="Quality"/> enum name, the <see cref="Root"/> pitch class, the CAGED
/// shape / shell form (<see cref="Shape"/>), and the optional neck window (<see cref="MinFret"/>/<see cref="MaxFret"/>,
/// absent ⇒ the full-neck default).
/// </summary>
public sealed record VoicingDeriveRequest(
    string? Family, string? Quality, int Root, string? Shape, int? MinFret, int? MaxFret);

/// <summary>
/// Parses inbound JSON envelopes from the WebView (JS→C#) and raises typed
/// events for feature slices to subscribe to. The envelope <c>type</c> string is
/// the only contract surface. Inbound vocabulary: <c>ready</c> /
/// <c>playbackFinished</c> / <c>beatChanged</c> / <c>generate</c> / <c>play</c> /
/// <c>stop</c> / <c>setTempo</c> / <c>save</c> / <c>listExercises</c> /
/// <c>loadExercise</c> / <c>markPracticed</c> / the generic content-CRUD family
/// <c>entityList</c> / <c>entityGet</c> / <c>entityPreview</c> / <c>entitySave</c> /
/// <c>entityDelete</c> (each carrying an <c>entity</c> discriminator) / the playback-soundfont pair
/// <c>listSoundFonts</c> / <c>setSoundFont</c> / the staff-display-profile pair <c>getStaffProfile</c> /
/// <c>setStaffProfile</c> / <c>scalePreview</c> / <c>cagedPreview</c> / <c>cagedChordPreview</c> / <c>voicingGrid</c> /
/// <c>voicingDerive</c> / <c>voicingOperators</c>.
/// </summary>
public sealed class WebMessageRouter
{
    // Web defaults: camelCase + case-insensitive property matching — mirrors the
    // JSON the JS side produces with JSON.stringify.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>WebView booted and alphaTab is ready to receive a score — carries the UI's initial render options.</summary>
    public event Action<RenderOptions>? Ready;

    /// <summary>Playback reached the end (player returned to the stopped state).</summary>
    public event Action? PlaybackFinished;

    /// <summary>Active beat advanced — <c>(bar, beat)</c>, both 1-based. For progress/accuracy later.</summary>
    public event Action<int, int>? BeatChanged;

    /// <summary>Generate a new exercise from the UI's chosen references + params — <c>(request, renderOptions)</c>.</summary>
    public event Action<GenerateRequest, RenderOptions>? GenerateRequested;

    /// <summary>Start/resume playback (routes to PracticeSession).</summary>
    public event Action? PlayRequested;

    /// <summary>Stop playback (routes to PracticeSession).</summary>
    public event Action? StopRequested;

    /// <summary>Set playback tempo in BPM (routes to PracticeSession).</summary>
    public event Action<int>? SetTempoRequested;

    /// <summary>Save the currently active exercise definition to the library.</summary>
    public event Action? SaveRequested;

    /// <summary>Send the saved-exercise list back to the WebView.</summary>
    public event Action? ListExercisesRequested;

    /// <summary>
    /// Reload a saved exercise by id and push its regenerated score — <c>(id, keyOverride?, tripletFeel?, renderOptions)</c>.
    /// The key/feel overrides are absent on a plain library click (⇒ the stored params seed ScoreR, C2) and present
    /// only on a live Key/Feel change ScoreR replays through onNeedsRerender (scorer-render-params IN4).
    /// </summary>
    public event Action<int, int?, bool?, TripletFeel?, RenderOptions>? LoadExerciseRequested;

    /// <summary>Record a practice event for the active exercise.</summary>
    public event Action? MarkPracticedRequested;

    /// <summary>List one content entity type's definitions — <c>(entity)</c>.</summary>
    public event Action<string>? EntityListRequested;

    /// <summary>Open one content definition for editing — <c>(entity, id)</c>.</summary>
    public event Action<string, string>? EntityGetRequested;

    /// <summary>
    /// Live-preview an unsaved content DSL — <c>(entity, dsl, renderOptions, tripletFeel, compingPatternId, keyPitchClass?, tempo?)</c>.
    /// Key/tempo are the ScoreR render params carried on the preview so the editor renders in the seeded key/tempo
    /// and a live change re-voices it, symmetric with Practice (scorer-render-params IN7); absent ⇒ the C / 80 default.
    /// </summary>
    public event Action<string, string, RenderOptions, TripletFeel, string?, int?, bool, int?>? EntityPreviewRequested;

    /// <summary>Create/update a content definition — <c>(entity, id?, name, dsl, sourceId?, tonality?)</c> (null id =
    /// create; sourceId = the fork-from item so its catalog header is preserved — EX3; tonality = the editor's
    /// explicit "major"/"minor" choice, which overrides the preserved tonality when present).</summary>
    public event Action<string, string?, string, string, string?, string?>? EntitySaveRequested;

    /// <summary>Delete (or revert) a content definition — <c>(entity, id)</c>.</summary>
    public event Action<string, string>? EntityDeleteRequested;

    /// <summary>List the available playback soundfonts (+ the persisted selection) back to the WebView.</summary>
    public event Action? ListSoundFontsRequested;

    /// <summary>Persist a new global playback soundfont choice — <c>(id)</c>.</summary>
    public event Action<string>? SetSoundFontRequested;

    /// <summary>Send the persisted staff-display profile (tab/standard/both) back to the WebView.</summary>
    public event Action? GetStaffProfileRequested;

    /// <summary>Persist a new global staff-display profile choice — <c>(profile)</c>.</summary>
    public event Action<string>? SetStaffProfileRequested;

    /// <summary>Preview an interval set on the fretboard (the Scales page) — <c>(intervals, rootPitchClass)</c>.</summary>
    public event Action<string, int>? ScalePreviewRequested;

    /// <summary>Preview a drum groove (the Drums page) — <c>(dsl, tempo)</c> → tex + grid diagram.</summary>
    public event Action<string, int>? DrumPreviewRequested;

    /// <summary>Preview a CAGED octave shape on the fretboard (the CAGED Shapes page) — <c>(shape, rootPitchClass)</c>.</summary>
    public event Action<string, int>? CagedPreviewRequested;

    /// <summary>Preview a derived CAGED chord on the fretboard (the CAGED Chords page) — <c>(family, shape, quality, rootPitchClass)</c>.</summary>
    public event Action<string, string, string, int>? CagedChordPreviewRequested;

    /// <summary>Resolve the whole filtered voicings grid in one round-trip (GuitarVoicingsR) — <c>(filter)</c>.</summary>
    public event Action<VoicingGridFilter>? VoicingGridRequested;

    /// <summary>Derive one voicing (+ its trace) for the Voicings Engine inspector page — <c>(request)</c>.</summary>
    public event Action<VoicingDeriveRequest>? VoicingDeriveRequested;

    /// <summary>Send the operator catalog (registry + declared schemas) to the Voicings Engine page.</summary>
    public event Action? VoicingOperatorsRequested;

    /// <summary>Export the on-screen chord sheet to PDF (host prints the print-styled page via WebView2).</summary>
    public event Action? ExportChordSheetPdfRequested;

    /// <summary>Deserialize one inbound message string and dispatch it to subscribers.</summary>
    public void Dispatch(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        InboundEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<InboundEnvelope>(message, JsonOptions);
        }
        catch (JsonException)
        {
            // A malformed envelope is a bridge bug, not a reason to crash the host.
            // Drop it and keep running; the symptom surfaces in the WebView console.
            return;
        }

        switch (envelope?.Type)
        {
            case "ready":
                Ready?.Invoke(ToRenderOptions(envelope.RenderOptions));
                break;
            case "playbackFinished":
                PlaybackFinished?.Invoke();
                break;
            case "beatChanged":
                BeatChanged?.Invoke(envelope.Bar ?? 0, envelope.Beat ?? 0);
                break;
            case "generate":
                GenerateRequested?.Invoke(
                    new GenerateRequest(
                        envelope.HarmonyEntity ?? "progression",
                        envelope.HarmonyId ?? "",
                        envelope.CompingPatternId ?? "beat_1_3",
                        envelope.LeadPatternId,
                        envelope.KeyPitchClass,
                        envelope.Tempo ?? 80,
                        ParseEnum(envelope.Difficulty, Difficulty.Beginner),
                        ParseEnum(envelope.TripletFeel, TripletFeel.None),
                        envelope.KeyIsMinor ?? false,
                        envelope.DrumGrooveId,
                        envelope.DrumVolume ?? 1.0),
                    ToRenderOptions(envelope.RenderOptions));
                break;
            case "play":
                PlayRequested?.Invoke();
                break;
            case "stop":
                StopRequested?.Invoke();
                break;
            case "setTempo":
                if (envelope.Bpm is int bpm)
                {
                    SetTempoRequested?.Invoke(bpm);
                }
                break;
            case "save":
                SaveRequested?.Invoke();
                break;
            case "listExercises":
                ListExercisesRequested?.Invoke();
                break;
            case "loadExercise":
                if (envelope.Id is int id)
                {
                    LoadExerciseRequested?.Invoke(
                        id, envelope.KeyPitchClass, envelope.KeyIsMinor, ParseNullableEnum<TripletFeel>(envelope.TripletFeel),
                        ToRenderOptions(envelope.RenderOptions));
                }
                break;
            case "markPracticed":
                MarkPracticedRequested?.Invoke();
                break;
            case "entityList":
                if (envelope.Entity is { } listEntity)
                {
                    EntityListRequested?.Invoke(listEntity);
                }
                break;
            case "entityGet":
                if (envelope.Entity is { } getEntity && envelope.EntityId is { } getId)
                {
                    EntityGetRequested?.Invoke(getEntity, getId);
                }
                break;
            case "entityPreview":
                if (envelope.Entity is { } prevEntity && envelope.Dsl is { } prevDsl)
                {
                    EntityPreviewRequested?.Invoke(
                        prevEntity, prevDsl, ToRenderOptions(envelope.RenderOptions),
                        ParseEnum(envelope.TripletFeel, TripletFeel.None), envelope.CompingPatternId,
                        envelope.KeyPitchClass, envelope.KeyIsMinor ?? false, envelope.Tempo);
                }
                break;
            case "entitySave":
                if (envelope.Entity is { } saveEntity && envelope.Name is { } saveName && envelope.Dsl is { } saveDsl)
                {
                    // EntityId is optional: null/absent means "create" (the store mints a GUID). SourceId (the
                    // fork-from item) is optional too — the store preserves its catalog header (tonality/…).
                    // Tonality (the editor's explicit choice) overrides the preserved tonality when present.
                    EntitySaveRequested?.Invoke(saveEntity, envelope.EntityId, saveName, saveDsl, envelope.SourceId, envelope.Tonality);
                }
                break;
            case "entityDelete":
                if (envelope.Entity is { } delEntity && envelope.EntityId is { } delId)
                {
                    EntityDeleteRequested?.Invoke(delEntity, delId);
                }
                break;
            case "listSoundFonts":
                ListSoundFontsRequested?.Invoke();
                break;
            case "setSoundFont":
                if (envelope.SoundFontId is { } soundFontId)
                {
                    SetSoundFontRequested?.Invoke(soundFontId);
                }
                break;
            case "getStaffProfile":
                GetStaffProfileRequested?.Invoke();
                break;
            case "setStaffProfile":
                if (envelope.Profile is { } staffProfile)
                {
                    SetStaffProfileRequested?.Invoke(staffProfile);
                }
                break;
            case "scalePreview":
                if (envelope.Intervals is { } scaleIntervals)
                {
                    ScalePreviewRequested?.Invoke(scaleIntervals, envelope.RootPitchClass ?? 0);
                }
                break;
            case "drumPreview":
                if (envelope.Dsl is { } drumDsl)
                {
                    DrumPreviewRequested?.Invoke(drumDsl, envelope.Tempo ?? 100);
                }
                break;
            case "cagedPreview":
                if (envelope.Shape is { } cagedShape)
                {
                    CagedPreviewRequested?.Invoke(cagedShape, envelope.RootPitchClass ?? 0);
                }
                break;
            case "cagedChordPreview":
                if (envelope.Shape is { } chordShape && envelope.Quality is { } chordQuality)
                {
                    CagedChordPreviewRequested?.Invoke(
                        envelope.Family ?? "caged", chordShape, chordQuality, envelope.RootPitchClass ?? 0);
                }
                break;
            case "voicingGrid":
                VoicingGridRequested?.Invoke(new VoicingGridFilter(
                    envelope.RootPitchClass ?? 0,
                    envelope.Sources, envelope.Families, envelope.Thirds, envelope.Fifths, envelope.Sevenths));
                break;
            case "voicingDerive":
                VoicingDeriveRequested?.Invoke(new VoicingDeriveRequest(
                    envelope.Family, envelope.Quality, envelope.RootPitchClass ?? 0,
                    envelope.Shape, envelope.MinFret, envelope.MaxFret));
                break;
            case "voicingOperators":
                VoicingOperatorsRequested?.Invoke();
                break;
            case "exportChordSheet":
                ExportChordSheetPdfRequested?.Invoke();
                break;
            // Unknown / null types are ignored — forward-compatible.
        }
    }

    // Map the optional inbound render-options object to the Core RenderOptions; absent ⇒ Default (today's render).
    private static RenderOptions ToRenderOptions(InboundRenderOptions? options)
    {
        if (options is null)
        {
            return RenderOptions.Default;
        }

        return new RenderOptions(
            ShowChordNames: options.ShowChordNames ?? false,
            ShowChordDiagramsOverStaff: options.ShowChordDiagramsOverStaff ?? false,
            ShowChordDiagramsOnTop: options.ShowChordDiagramsOnTop ?? false,
            Voicing: ParseVoicingSource(options.Voicing));
    }

    // The comping voicing source (engine-derived-as-app-source IN6): a structured practice knob. Absent ⇒ null
    // ⇒ the resolver's default (automatic / full neck / Closest). The kind is normalized; unknown kinds/rankings
    // fail loud in the resolver, not here.
    private static VoicingSource? ParseVoicingSource(InboundVoicingSource? voicing) =>
        voicing is null
            ? null
            : new VoicingSource(
                Kind: string.IsNullOrWhiteSpace(voicing.Kind) ? VoicingSource.Automatic : voicing.Kind.Trim().ToLowerInvariant(),
                MinFret: voicing.MinFret,
                MaxFret: voicing.MaxFret,
                PackageId: voicing.PackageId,
                Ranking: voicing.Ranking);

    // Parse a string enum param (Difficulty/TripletFeel) case-insensitively; an absent or unrecognized value falls
    // back to the supplied default (forward-compatible — a new value the host doesn't know is ignored).
    private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out T parsed) && Enum.IsDefined(parsed) ? parsed : fallback;

    // Nullable enum parse for an OPTIONAL override param: absent/unrecognized ⇒ null (distinct from a default),
    // so a plain loadExercise (no override) leaves the stored value in force while a replayed live change wins.
    private static T? ParseNullableEnum<T>(string? value) where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out T parsed) && Enum.IsDefined(parsed) ? parsed : (T?)null;

    private sealed record InboundEnvelope(
        string? Type, int? Bar, int? Beat, int? Id,
        // generate references + params: a song/progression harmony discriminator + id, the comping pattern id,
        // an optional lead pattern id, the chosen key (null → the Song's own key), tempo, and the Difficulty/TripletFeel
        // param values (enum names). KeyPitchClass/Tempo are reused by setTempo's Bpm sibling below.
        string? HarmonyEntity, string? HarmonyId, string? CompingPatternId, string? LeadPatternId,
        int? KeyPitchClass, bool? KeyIsMinor, int? Tempo, string? Difficulty, string? TripletFeel, int? Bpm,
        // generate: the optional drum-groove reference tiled beneath the harmony + its saved mix volume
        // (drums-under-a-song IN4). Absent ⇒ no drum part.
        string? DrumGrooveId, double? DrumVolume,
        // Content-CRUD fields: Entity discriminator, the string content id (distinct from the int Id used by
        // loadExercise), the editor's Name/Dsl payload, SourceId — the fork-from item whose catalog header
        // (genre/tags/description/tonality) a save preserves onto the new user copy (EX3) — and Tonality, the
        // editor tonality control's explicit "major"/"minor" choice (overrides the preserved tonality).
        string? Entity, string? EntityId, string? Name, string? Dsl, string? SourceId, string? Tonality,
        // setSoundFont: the chosen soundfont id (file name). A string, distinct from the int Id / string EntityId.
        string? SoundFontId,
        // setStaffProfile: the chosen staff-display profile ("tab"/"standard"/"both"). getStaffProfile carries none.
        string? Profile,
        // scalePreview: the interval set text ("1 b3 4 5 b7") + the chosen root pitch class (0..11).
        string? Intervals, int? RootPitchClass,
        // cagedPreview: the CAGED shape name ("C"/"A"/"G"/"E"/"D"); reuses RootPitchClass for the root.
        // cagedChordPreview adds Quality (the quality enum name) and Family ("caged"/"dshell"/"shell") alongside Shape + RootPitchClass.
        string? Shape, string? Quality, string? Family,
        // voicingGrid: the faceted filter state — reuses RootPitchClass for the single root; the rest are the
        // multi-select enabled-token sets per level (absent ⇒ null ⇒ that level is unconstrained).
        IReadOnlyList<string>? Sources, IReadOnlyList<string>? Families, IReadOnlyList<string>? Thirds,
        IReadOnlyList<string>? Fifths, IReadOnlyList<string>? Sevenths,
        // voicingDerive: the neck window for a single-operator derivation (reuses Family/Quality/Shape/RootPitchClass).
        int? MinFret, int? MaxFret,
        // Optional render-time presentation options on the render-producing verbs (generate/loadExercise/entityPreview).
        InboundRenderOptions? RenderOptions);

    private sealed record InboundRenderOptions(
        bool? ShowChordNames, bool? ShowChordDiagramsOverStaff, bool? ShowChordDiagramsOnTop, InboundVoicingSource? Voicing);

    // The structured comping voicing knob on renderOptions.voicing (IN6): { kind, minFret, maxFret, packageId, ranking }.
    private sealed record InboundVoicingSource(
        string? Kind, int? MinFret, int? MaxFret, string? PackageId, string? Ranking);
}

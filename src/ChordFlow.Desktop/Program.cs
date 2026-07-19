using ChordFlow.Exercises;
using ChordFlow.Music.Rhythm;
using ChordFlow.Instruments.Guitar;
using ChordFlow.Features.ContentCrud;
using ChordFlow.Features.Drums;
using ChordFlow.Features.ExerciseLibrary;
using ChordFlow.Features.GenerateExercise;
using ChordFlow.Features.Packs;
using ChordFlow.Features.PracticeSession;
using ChordFlow.Features.Progress;
using ChordFlow.Features.Scales;
using ChordFlow.Features.Caged;
using ChordFlow.Features.ChordSheets;
using ChordFlow.Features.Voicings;
using ChordFlow.Features;
using ChordFlow.Bridge;
using ChordFlow.Persistence;
using ChordFlow.Desktop.WebHost;
using ChordFlow.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

// ChordFlow desktop host (WinForms + WebView2).
//
// A WinForms window hosts the official WebView2 control (windowed controller —
// the path that renders on this net10 + WebView2-149 stack, where Photino's
// composition controller produced a black window). wwwroot is served over a
// virtual-host https origin via SetVirtualHostNameToFolderMapping: no HTTP
// server, no localhost port (C2), and a real origin so alphaTab's soundfont
// fetch isn't CORS-blocked the way a file:// page would be.
//
// The narrow C#<->JS bridge (req IN8) is unchanged in contract: WebView2Bridge
// sends envelopes out, WebMessageRouter parses them in. On "ready" the
// GenerateExercise slice pushes a real engine-produced score.

internal static class Program
{
    private const string VirtualHost = "chordflow.local";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var form = new Form
        {
            Text = "ChordFlow",
            Width = 1100,
            Height = 820,
            StartPosition = FormStartPosition.CenterScreen,
        };

        // Window/taskbar icon — reuse the embedded application icon (set via
        // <ApplicationIcon> in ChordFlow.Desktop.csproj) so the form matches the exe.
        try { form.Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!); }
        catch { /* fall back to the default WinForms icon */ }

        var web = new WebView2 { Dock = DockStyle.Fill };
        form.Controls.Add(web);

        form.Load += async (_, _) =>
        {
            try
            {
                await web.EnsureCoreWebView2Async();
                CoreWebView2 core = web.CoreWebView2;

                // Debug hooks (default OFF; kept in-tree for future bug hunts). Set the CHORDFLOW_DEVTOOLS env
                // var to enable WebView devtools (F12/Console) AND expose window.__cfApi / window.__cfEngine in
                // the JS (score-render-component.js gates on window.__cfDebug). Inert in normal runs.
                bool debugHooks = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CHORDFLOW_DEVTOOLS"));
                core.Settings.AreDevToolsEnabled = debugHooks;
                if (debugHooks)
                {
                    await core.AddScriptToExecuteOnDocumentCreatedAsync("window.__cfDebug = true;");
                }

                // wwwroot is copied next to the executable (see ChordFlow.Desktop.csproj).
                string wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                core.SetVirtualHostNameToFolderMapping(
                    VirtualHost, wwwroot, CoreWebView2HostResourceAccessKind.Allow);

                // SQLite store (constraint C2: one local file, no server). Apply
                // migrations on startup so the schema exists on first run.
                DbContextOptions<ChordFlowDbContext> dbOptions =
                    new DbContextOptionsBuilder<ChordFlowDbContext>()
                        .UseSqlite($"Data Source={ChordFlowDbContext.DefaultDbPath()}")
                        .Options;
                using (var db = new ChordFlowDbContext(dbOptions))
                {
                    db.Database.Migrate();
                    // Import the free starter content on first run from the on-disk default pack
                    // (idempotent by (Id, Origin); content is data, not code — IN6). It now imports as
                    // Origin.Pack (PackId "default") — the default pack is an ordinary package.
                    DefaultPack.ImportInto(db);
                    // Retire the legacy BuiltIn tier + fork legacy user shadows into unique-id copies
                    // (content-source-model). Idempotent — a no-op once migrated.
                    ContentSourceMigration.Run(db);
                }

                // Bridge wiring — same envelope contract, WebView2 transport. Build it
                // before navigating so the JS "ready" ping is never missed.
                var router = new WebMessageRouter();
                var bridge = new WebView2Bridge(core, router);
                // The renderer is a pure formatter (engine-derived-as-app-source D4=(B)): voicing selection
                // happens per-render in the Features comping resolver from a freshly read voicing source, so an
                // authored-voicing change takes effect on the next render with no hot-swap.
                var renderer = new AlphaTexRenderer();
                var generate = new GenerateExerciseHandler(dbOptions, renderer);
                var library = new ExerciseLibraryHandler(dbOptions, renderer);
                var progress = new ProgressHandler(dbOptions);
                // PackId → display-name map for source tagging (content-source-model IN2). Only the default
                // pack exists today (EX3 — no pack-management UI); read its manifest name once at startup.
                var packNames = new Dictionary<string, string>
                {
                    [DefaultPack.PackId] = DefaultPack.Load().Manifest.Name,
                };
                // The engine-derived `automatic` voicing source fills the content-source-model union seam, so
                // the 36 CAGED families list on the Content page alongside package + user voicings (IN2).
                var contentCrud = new ContentCrudHandler(dbOptions, renderer, packNames, new EngineVoicingSource());
                var scales = new ScalesHandler();
                var drumPreview = new DrumGroovePreviewHandler();
                var caged = new CagedShapesHandler();
                var cagedChord = new CagedChordHandler();
                var voicingGrid = new VoicingGridHandler();
                var voicingDerive = new VoicingDeriveHandler();

                // App-lifetime global-preference store (key/value over SQLite). Shared by the soundfont choice
                // and the staff-display profile below.
                var appSettings = new AppSettingsStore(dbOptions);

                // Playback soundfont library: the catalog scans the served wwwroot/soundfont folder (host asset),
                // and the global choice persists via the Core AppSettings store (C3). App-lifetime singletons.
                var soundFonts = new SoundFontLibrary(
                    new WwwrootSoundFontCatalog(Path.Combine(wwwroot, "soundfont")),
                    appSettings);

                // PracticeSession is the C# transport seam (drives play/stop/tempo,
                // tracks position from playbackFinished/beatChanged echoes).
                _ = new PracticeSessionHandler(bridge, router);

                // Shared session state. currentExercise = the definition on screen;
                // activeExerciseId = its DB id once saved/loaded (null = unsaved, so
                // Save inserts; non-null = already persisted, so Save is a no-op).
                Exercise? currentExercise = null;
                int? activeExerciseId = null;

                // Render an exercise and push its score; on a render failure surface a status
                // to the UI instead of silently dropping the message (which looked like the
                // control "did nothing"). Returns whether the score was sent.
                bool TrySendScore(Exercise exercise, RenderOptions options)
                {
                    try
                    {
                        // Expansion (the one I/O seam) needs the progression store; a short-lived context
                        // per render keeps it consistent with the other handlers (merge decision (a)).
                        using var renderDb = new ChordFlowDbContext(dbOptions);
                        bridge.Send(LoadScoreEnvelope.From(
                            exercise, new ProgressionStore(renderDb), renderer,
                            StoredVoicingSource.From(new VoicingStore(renderDb)), options,
                            references: VoicingReferenceSource.From(new VoicingStore(renderDb))));
                        return true;
                    }
                    catch (Exception renderEx)
                    {
                        bridge.Send(new StatusEnvelope($"Couldn't render this exercise: {renderEx.Message}", true));
                        return false;
                    }
                }

                // When the WebView reports it booted, push a real engine-produced score.
                // MVP default: 12-bar blues in C (pitch class 0), "Beats 1 & 3", 80 BPM.
                router.Ready += renderOptions =>
                {
                    Exercise boot = generate.Build(
                        harmonyEntity: "progression", harmonyId: "12bar_blues", compingPatternId: "beat_1_3",
                        leadPatternId: null, keyPitchClass: 0, tempo: 80,
                        difficulty: Difficulty.Beginner, tripletFeel: TripletFeel.None);
                    if (TrySendScore(boot, renderOptions))
                    {
                        currentExercise = boot;
                        activeExerciseId = null;
                    }
                    bridge.Send(library.List()); // populate the saved-exercise list on boot
                };

                // Generate a fresh exercise from the UI's chosen content references + params. A bad/missing
                // reference throws in Build, surfaced as a status (not a silent no-op). It becomes the on-screen
                // (unsaved) definition only if it built and rendered.
                router.GenerateRequested += (req, renderOptions) =>
                {
                    try
                    {
                        Exercise exercise = generate.Build(
                            req.HarmonyEntity, req.HarmonyId, req.CompingPatternId, req.LeadPatternId,
                            req.KeyPitchClass, req.Tempo, req.Difficulty, req.TripletFeel, req.KeyIsMinor,
                            req.DrumGrooveId, req.DrumVolume);
                        if (TrySendScore(exercise, renderOptions))
                        {
                            currentExercise = exercise;
                            activeExerciseId = null;
                        }
                    }
                    catch (Exception genEx)
                    {
                        bridge.Send(new StatusEnvelope($"Couldn't generate this exercise: {genEx.Message}", true));
                    }
                };

                // Save the on-screen definition (only if unsaved), then refresh the list.
                router.SaveRequested += () =>
                {
                    if (currentExercise is not null && activeExerciseId is null)
                    {
                        activeExerciseId = library.Save(currentExercise);
                        bridge.Send(library.List());
                    }
                };

                router.ListExercisesRequested += () => bridge.Send(library.List());

                // Reload a saved exercise: regenerated score + becomes the active definition. A live Key/Feel
                // change ScoreR replays carries a transient keyOverride/tripletFeel that re-voices the displayed
                // piece without touching the stored definition (scorer-render-params IN4/C2).
                router.LoadExerciseRequested += (id, keyOverride, keyIsMinor, tripletFeel, renderOptions) =>
                {
                    try
                    {
                        LoadedExercise? loaded = library.Load(id, renderOptions, keyOverride, tripletFeel, keyIsMinor);
                        if (loaded is not null)
                        {
                            currentExercise = loaded.Exercise;
                            activeExerciseId = id;
                            bridge.Send(loaded.Score);
                        }
                    }
                    catch (Exception loadEx)
                    {
                        bridge.Send(new StatusEnvelope($"Couldn't load this exercise: {loadEx.Message}", true));
                    }
                };

                // Mark practiced: an unsaved exercise is saved first (marking it practiced
                // is a clear signal to keep it), then a practice event is recorded.
                router.MarkPracticedRequested += () =>
                {
                    if (currentExercise is null)
                    {
                        return;
                    }

                    if (activeExerciseId is null)
                    {
                        activeExerciseId = library.Save(currentExercise);
                        bridge.Send(library.List());
                    }

                    int count = progress.MarkPracticed(activeExerciseId.Value);
                    bridge.Send(new PracticeRecordedEnvelope(activeExerciseId.Value, count));
                    bridge.Send(library.List()); // refresh so the ✓ practiced marker appears
                };

                // Content-CRUD wiring (the generic entity* protocol). Failures from a bad DSL surface as an
                // entityParseError shown inline (IN3); list/get failures (a bogus entity) become a status line.
                router.EntityListRequested += entity =>
                {
                    try { bridge.Send(contentCrud.List(entity)); }
                    catch (FormatException ex) { bridge.Send(new StatusEnvelope(ex.Message, true)); }
                };
                router.EntityGetRequested += (entity, id) =>
                {
                    try
                    {
                        EntityLoadedEnvelope? loaded = contentCrud.Get(entity, id);
                        if (loaded is not null)
                        {
                            bridge.Send(loaded);
                        }
                        else
                        {
                            bridge.Send(new StatusEnvelope($"'{id}' not found.", true));
                        }
                    }
                    catch (FormatException ex) { bridge.Send(new StatusEnvelope(ex.Message, true)); }
                };
                router.EntityPreviewRequested += (entity, dsl, renderOptions, tripletFeel, compingPatternId, keyPitchClass, keyIsMinor, tempo) =>
                {
                    try { bridge.Send(contentCrud.Preview(entity, dsl, renderOptions, tripletFeel, compingPatternId, keyPitchClass, keyIsMinor, tempo)); }
                    catch (FormatException ex) { bridge.Send(new EntityParseErrorEnvelope(entity, ex.Message)); }
                };
                router.EntitySaveRequested += (entity, id, name, dsl, sourceId, tonality) =>
                {
                    try
                    {
                        bridge.Send(contentCrud.Save(entity, id, name, dsl, sourceId, tonality));
                        bridge.Send(contentCrud.List(entity)); // refresh the list (and badges)
                    }
                    catch (FormatException ex) { bridge.Send(new EntityParseErrorEnvelope(entity, ex.Message)); }
                };
                router.EntityDeleteRequested += (entity, id) =>
                {
                    try
                    {
                        bridge.Send(contentCrud.Delete(entity, id));
                        bridge.Send(contentCrud.List(entity));
                    }
                    catch (FormatException ex) { bridge.Send(new EntityParseErrorEnvelope(entity, ex.Message)); }
                };

                // Scales page: build the interval-set fretboard diagram; a bad token surfaces inline (scaleError).
                router.ScalePreviewRequested += (intervals, rootPc) =>
                {
                    try { bridge.Send(scales.Preview(intervals, rootPc)); }
                    catch (FormatException ex) { bridge.Send(new ScaleErrorEnvelope(ex.Message)); }
                };

                // Drums page: parse the hit-grid DSL → percussion tex + grid diagram (one parse, two projections);
                // a bad DSL surfaces inline as drumPreviewError (mirrors the scale/CRUD parse-error path).
                router.DrumPreviewRequested += (dsl, tempo) =>
                {
                    try { bridge.Send(drumPreview.Preview(dsl, tempo)); }
                    catch (FormatException ex) { bridge.Send(new DrumPreviewErrorEnvelope(ex.Message)); }
                };

                // CAGED Shapes page: build the octave-shape fretboard diagram; an unknown shape surfaces inline (cagedError).
                router.CagedPreviewRequested += (shape, rootPc) =>
                {
                    try { bridge.Send(caged.Preview(shape, rootPc)); }
                    catch (FormatException ex) { bridge.Send(new CagedErrorEnvelope(ex.Message)); }
                };

                // CAGED Chords page: derive the grip + build its diagram; an unknown or unvoiceable combo surfaces inline.
                router.CagedChordPreviewRequested += (family, shape, quality, rootPc) =>
                {
                    try { bridge.Send(cagedChord.Preview(family, quality, shape, rootPc)); }
                    catch (Exception ex) when (ex is FormatException or InvalidOperationException or ArgumentOutOfRangeException)
                    { bridge.Send(new CagedChordErrorEnvelope(ex.Message)); }
                };

                // GuitarVoicingsR page: resolve the whole filtered voicings grid in one round-trip. Unvoiceable
                // combos are dropped inside the handler, so an over-narrow filter just yields fewer/zero cells.
                router.VoicingGridRequested += filter => bridge.Send(voicingGrid.Build(filter));

                // Voicings Engine inspector page: the operator catalog (schema-driven controls), and one derivation
                // (abstract voicing + steps + grip) per request. Invalid input fails loud into a UI-safe error reply.
                router.VoicingOperatorsRequested += () => bridge.Send(voicingDerive.Operators());
                router.VoicingDeriveRequested += request =>
                {
                    try
                    {
                        bridge.Send(voicingDerive.Derive(request));
                    }
                    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
                    {
                        bridge.Send(new VoicingDeriveErrorEnvelope(ex.Message));
                    }
                };

                // The chord-sheet MODEL has no request of its own anymore: it rides the unified loadScore reply
                // as a projection of the generate/loadExercise pass (harmony-controls-r IN3). Only the PDF
                // print round-trip below remains sheet-specific.

                // Export the on-screen chord sheet to PDF: the page injects a print-styled light copy into
                // #chord-sheet-print (an @media print rule hides everything else), then the host prints the current
                // page via WebView2's native PrintToPdfAsync — no external PDF library (C4). A cancel replies Ok=false
                // so the page always tears its print container back down.
                router.ExportChordSheetPdfRequested += async () =>
                {
                    try
                    {
                        using var dialog = new SaveFileDialog
                        {
                            Filter = "PDF document (*.pdf)|*.pdf",
                            FileName = "chord-sheet.pdf",
                            DefaultExt = "pdf",
                            AddExtension = true,
                        };
                        if (dialog.ShowDialog() != DialogResult.OK)
                        {
                            bridge.Send(new ChordSheetPdfDoneEnvelope(false));
                            return;
                        }

                        await core.PrintToPdfAsync(dialog.FileName, null);
                        bridge.Send(new ChordSheetPdfDoneEnvelope(true, dialog.FileName));
                    }
                    catch (Exception ex)
                    {
                        bridge.Send(new ChordSheetPdfDoneEnvelope(false, null, ex.Message));
                    }
                };

                // Playback soundfont: list (fonts + persisted selection) on request; persist a new global choice.
                router.ListSoundFontsRequested += () => bridge.Send(soundFonts.ListWithSelection());
                router.SetSoundFontRequested += id => soundFonts.SetSelected(id);

                // Staff-display profile (tab/standard/both): a display-only score-view preference, persisted
                // globally via the same AppSettings store as the soundfont choice (C2/C6). Default "tab" (IN2).
                const string StaffProfileKey = "display.staffProfile";
                router.GetStaffProfileRequested += () => bridge.Send(new StaffProfileEnvelope(appSettings.Get(StaffProfileKey) ?? "tab"));
                router.SetStaffProfileRequested += profile => appSettings.Set(StaffProfileKey, profile);

                core.Navigate($"https://{VirtualHost}/index.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize the WebView:\n\n{ex}",
                    "ChordFlow", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        Application.Run(form);
    }
}

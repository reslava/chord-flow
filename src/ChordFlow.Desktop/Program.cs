using ChordFlow.Instruments.Guitar;
using ChordFlow.Domain;
using ChordFlow.Features.ContentCrud;
using ChordFlow.Features.ExerciseLibrary;
using ChordFlow.Features.GenerateExercise;
using ChordFlow.Features.Packs;
using ChordFlow.Features.PracticeSession;
using ChordFlow.Features.Progress;
using ChordFlow.Features.Scales;
using ChordFlow.Features.Caged;
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

        var web = new WebView2 { Dock = DockStyle.Fill };
        form.Controls.Add(web);

        form.Load += async (_, _) =>
        {
            try
            {
                await web.EnsureCoreWebView2Async();
                CoreWebView2 core = web.CoreWebView2;

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
                IReadOnlyList<VoicingShape> voicingLibrary;
                using (var db = new ChordFlowDbContext(dbOptions))
                {
                    db.Database.Migrate();
                    // Import the free starter content on first run from the on-disk default pack
                    // (idempotent by (Id, Origin); content is data, not code — IN6).
                    DefaultPack.ImportInto(db);
                    // Authored-voicing library, loaded once at startup; stored voicings shadow the generated
                    // shapes when rendering. (Voicings authored later take effect on the next launch — slice 1.)
                    voicingLibrary = new VoicingStore(db).LoadShapes();
                }

                // Bridge wiring — same envelope contract, WebView2 transport. Build it
                // before navigating so the JS "ready" ping is never missed.
                var router = new WebMessageRouter();
                var bridge = new WebView2Bridge(core, router);
                // Swappable so an authored-voicing change can hot-rebuild the voicing book without a restart (IN11).
                var renderer = new SwappableRenderer(new AlphaTexRenderer(new VoicingBook(voicingLibrary)));
                var generate = new GenerateExerciseHandler(dbOptions, renderer);
                var library = new ExerciseLibraryHandler(dbOptions, renderer);
                var progress = new ProgressHandler(dbOptions);
                var contentCrud = new ContentCrudHandler(dbOptions, renderer);
                var scales = new ScalesHandler();
                var caged = new CagedShapesHandler();

                // Playback soundfont library: the catalog scans the served wwwroot/soundfont folder (host asset),
                // and the global choice persists via the Core AppSettings store (C3). App-lifetime singletons.
                var soundFonts = new SoundFontLibrary(
                    new WwwrootSoundFontCatalog(Path.Combine(wwwroot, "soundfont")),
                    new AppSettingsStore(dbOptions));

                // Live-refresh: after a voicing save/delete, reload the authored library and swap in a fresh
                // renderer so the next generated/previewed score reflects it (IN11). Progression/song/rhythm
                // are read per-use and aren't snapshotted, so they need no rebuild.
                contentCrud.VoicingsChanged += () =>
                {
                    using var rebuildDb = new ChordFlowDbContext(dbOptions);
                    renderer.Swap(new AlphaTexRenderer(new VoicingBook(new VoicingStore(rebuildDb).LoadShapes())));
                };

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
                        bridge.Send(LoadScoreEnvelope.From(exercise, new ProgressionStore(renderDb), renderer, options));
                        return true;
                    }
                    catch (Exception renderEx)
                    {
                        bridge.Send(new StatusEnvelope($"Couldn't render this exercise: {renderEx.Message}", true));
                        return false;
                    }
                }

                // When the WebView reports it booted, push a real engine-produced score.
                // MVP default: 12-bar blues in Bb (pitch class 10), "Beats 1 & 3", 80 BPM.
                router.Ready += renderOptions =>
                {
                    Exercise boot = generate.Build(
                        harmonyEntity: "progression", harmonyId: "12bar_blues", compingPatternId: "beat_1_3",
                        leadPatternId: null, keyPitchClass: 10, tempo: 80,
                        difficulty: Difficulty.Beginner, feel: Feel.Straight);
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
                            req.KeyPitchClass, req.Tempo, req.Difficulty, req.Feel);
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

                // Reload a saved exercise: regenerated score + becomes the active definition.
                router.LoadExerciseRequested += (id, renderOptions) =>
                {
                    try
                    {
                        LoadedExercise? loaded = library.Load(id, renderOptions);
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
                router.EntityPreviewRequested += (entity, dsl, renderOptions) =>
                {
                    try { bridge.Send(contentCrud.Preview(entity, dsl, renderOptions)); }
                    catch (FormatException ex) { bridge.Send(new EntityParseErrorEnvelope(entity, ex.Message)); }
                };
                router.EntitySaveRequested += (entity, id, name, dsl) =>
                {
                    try
                    {
                        bridge.Send(contentCrud.Save(entity, id, name, dsl));
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

                // CAGED Shapes page: build the octave-shape fretboard diagram; an unknown shape surfaces inline (cagedError).
                router.CagedPreviewRequested += (shape, rootPc) =>
                {
                    try { bridge.Send(caged.Preview(shape, rootPc)); }
                    catch (FormatException ex) { bridge.Send(new CagedErrorEnvelope(ex.Message)); }
                };

                // Playback soundfont: list (fonts + persisted selection) on request; persist a new global choice.
                router.ListSoundFontsRequested += () => bridge.Send(soundFonts.ListWithSelection());
                router.SetSoundFontRequested += id => soundFonts.SetSelected(id);

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

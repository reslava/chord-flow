using ChordFlow.Domain;
using ChordFlow.Features.ExerciseLibrary;
using ChordFlow.Features.GenerateExercise;
using ChordFlow.Features.PracticeSession;
using ChordFlow.Features.Progress;
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
                using (var db = new ChordFlowDbContext(dbOptions))
                {
                    db.Database.Migrate();
                    // Seed the built-in default progressions on first run (idempotent by Id).
                    db.SeedBuiltInProgressions();
                }

                // Bridge wiring — same envelope contract, WebView2 transport. Build it
                // before navigating so the JS "ready" ping is never missed.
                var router = new WebMessageRouter();
                var bridge = new WebView2Bridge(core, router);
                var renderer = new AlphaTexRenderer();
                var generate = new GenerateExerciseHandler(renderer);
                var library = new ExerciseLibraryHandler(dbOptions, renderer);
                var progress = new ProgressHandler(dbOptions);

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
                bool TrySendScore(Exercise exercise)
                {
                    try
                    {
                        bridge.Send(LoadScoreEnvelope.From(exercise, renderer));
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
                router.Ready += () =>
                {
                    Exercise boot = generate.Build(keyPitchClass: 10, rhythmId: "beat_1_3", tempo: 80);
                    if (TrySendScore(boot))
                    {
                        currentExercise = boot;
                        activeExerciseId = null;
                    }
                    bridge.Send(library.List()); // populate the saved-exercise list on boot
                };

                // Generate a fresh exercise from the UI's key/rhythm/tempo selections.
                // It becomes the on-screen (unsaved) definition only if it rendered.
                router.GenerateRequested += (keyPitchClass, rhythmId, tempo) =>
                {
                    Exercise exercise = generate.Build(keyPitchClass, rhythmId, tempo);
                    if (TrySendScore(exercise))
                    {
                        currentExercise = exercise;
                        activeExerciseId = null;
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
                router.LoadExerciseRequested += id =>
                {
                    try
                    {
                        LoadedExercise? loaded = library.Load(id);
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

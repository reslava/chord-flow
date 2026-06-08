using ChordFlow.Features.GenerateExercise;
using ChordFlow.Features.PracticeSession;
using ChordFlow.Infrastructure;
using ChordFlow.Rendering;
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

                // wwwroot is copied next to the executable (see ChordFlow.App.csproj).
                string wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                core.SetVirtualHostNameToFolderMapping(
                    VirtualHost, wwwroot, CoreWebView2HostResourceAccessKind.Allow);

                // Bridge wiring — same envelope contract, WebView2 transport. Build it
                // before navigating so the JS "ready" ping is never missed.
                var router = new WebMessageRouter();
                var bridge = new WebView2Bridge(core, router);
                var generate = new GenerateExerciseHandler(new AlphaTexRenderer());

                // PracticeSession is the C# transport seam (drives play/stop/tempo,
                // tracks position from playbackFinished/beatChanged echoes).
                _ = new PracticeSessionHandler(bridge, router);

                // When the WebView reports it booted, push a real engine-produced score.
                // MVP default: 12-bar blues in Bb (pitch class 10), "Beats 1 & 3", 80 BPM.
                router.Ready += () =>
                {
                    LoadScoreEnvelope score = generate.Generate(keyPitchClass: 10, rhythmId: "beat_1_3", tempo: 80);
                    bridge.Send(score);
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

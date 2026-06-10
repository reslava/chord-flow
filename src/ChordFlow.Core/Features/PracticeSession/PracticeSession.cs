using ChordFlow.Infrastructure;

namespace ChordFlow.Features.PracticeSession;

// Outbound transport envelopes (C#→JS). Each serializes to {"type":"…", …} via
// the bridge's camelCase JSON. Default Type keeps call sites terse.
public sealed record PlayCommand(string Type = "play");
public sealed record StopCommand(string Type = "stop");
public sealed record SetTempoCommand(int Bpm, string Type = "setTempo");

/// <summary>
/// PracticeSession vertical slice: drives play/stop/tempo over the bridge and
/// tracks playback state echoed back from alphaTab (playbackFinished, beatChanged).
/// No accuracy detection in v1 — this is the transport + position seam that the
/// later Progress slice and any future accuracy feature build on.
/// </summary>
public sealed class PracticeSessionHandler
{
    private readonly IBridge _bridge;

    public PracticeSessionHandler(IBridge bridge, WebMessageRouter router)
    {
        _bridge = bridge;

        // Inbound transport requests from the UI route here (each control posts an
        // envelope to its slice): Play/Stop/SetTempo echo the matching command back
        // to alphaTab via the bridge, so this slice owns the playback state.
        router.PlayRequested += Play;
        router.StopRequested += Stop;
        router.SetTempoRequested += SetTempo;

        // Inbound playback echoes from the WebView.
        router.PlaybackFinished += OnPlaybackFinished;
        router.BeatChanged += OnBeatChanged;
    }

    /// <summary>True while alphaTab reports playback running.</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>Last reported cursor position (1-based). 0 = none yet.</summary>
    public int CurrentBar { get; private set; }
    public int CurrentBeat { get; private set; }

    /// <summary>Start/resume playback in the WebView.</summary>
    public void Play()
    {
        IsPlaying = true;
        _bridge.Send(new PlayCommand());
    }

    /// <summary>Stop playback and reset to the start.</summary>
    public void Stop()
    {
        IsPlaying = false;
        _bridge.Send(new StopCommand());
    }

    /// <summary>Set playback tempo (BPM); alphaTab scales it off the score's authored tempo.</summary>
    public void SetTempo(int bpm) => _bridge.Send(new SetTempoCommand(bpm));

    private void OnPlaybackFinished()
    {
        IsPlaying = false;
        CurrentBar = 0;
        CurrentBeat = 0;
    }

    private void OnBeatChanged(int bar, int beat)
    {
        CurrentBar = bar;
        CurrentBeat = beat;
    }
}

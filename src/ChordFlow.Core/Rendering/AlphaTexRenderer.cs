using ChordFlow.Music.Progressions;
using ChordFlow.Exercises;
using ChordFlow.Music.Harmony;
using ChordFlow.Music.Rhythm;
using ChordFlow.Music.Songs;
using System.Globalization;
using System.Text;

using ChordFlow.Instruments.Guitar;

namespace ChordFlow.Rendering;

/// <summary>
/// The <b>only</b> component that knows alphaTex syntax. Turns a <see cref="RealizedSong"/> into an
/// alphaTex string per loom/refs/alphatex-syntax-reference.md: header metadata
/// (<c>\title \subtitle \tempo \ts \ks</c>), a lone <c>.</c> to end the header, then bars of stateful
/// <c>:N</c> durations, <c>( )</c> chord groups, <c>r</c> rests, separated by <c>|</c>. Note spelling
/// comes from <see cref="NoteSpeller"/> and rhythm tokens from <see cref="RhythmQuantizer"/> — this
/// type only formats them.
/// <para>
/// Chords are no longer 1:1 with bars (harmonic-rhythm layer): each bar is realized into ordered
/// <see cref="RealizedSpan"/>s and the rhythm is quantized with that bar's chord boundaries, so every
/// slot is mapped to the chord covering its <see cref="RhythmSlot.StartTick"/>. A single-chord bar has
/// no interior boundaries, so it reduces to the original one-chord-per-bar output (C4/C6).
/// </para>
/// </summary>
public sealed class AlphaTexRenderer : IScoreRenderer
{
    // A pure formatter (D4=(B)): voicing selection happens in the Features comping resolver and arrives as a
    // CompingPlan; the renderer holds no voicing state, so authored-voicing changes take effect on the next
    // render without any hot-swap.

    public RenderResult Render(RealizedSong song, RhythmPattern rhythm, int tempo, Difficulty difficulty, CompingPlan compingPlan, TripletFeel tripletFeel = TripletFeel.None, RhythmPattern? lead = null, RenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(song);
        ArgumentNullException.ThrowIfNull(rhythm);
        ArgumentNullException.ThrowIfNull(compingPlan);
        RenderOptions opts = options ?? RenderOptions.Default;
        if (song.Sections.Count == 0)
        {
            throw new ArgumentException("Cannot render a song with no sections.", nameof(song));
        }

        TimeSignature ts = rhythm.TimeSignature;

        // Seeded from the first section's key (\ks is legal mid-score, so later key changes are emitted
        // inline — no per-key score splitting; design §8.3).
        RealizedSection first = song.Sections[0];
        EnsureMajorSupported(first.Key);

        // Comping (rhythm-guitar) track body: pickup + section bars, with inline \ks on key change. The state
        // collects each \chord diagram definition once for the score metadata block.
        var state = new RenderState { Plan = compingPlan };
        List<string> compingBars = BuildCompingBars(song, rhythm, ts, opts, state);
        // Whole-song swing is delegated to alphaTab: emit one \tf on the first bar (None ⇒ nothing).
        PrependTripletFeel(compingBars, tripletFeel);

        string title = $"{first.Label} — {NoteSpeller.Name(first.Key.Tonic, first.Key)}";
        string subtitle = $"{difficulty} — {rhythm.Name}";
        string keySig = NoteSpeller.KeySignatureToken(first.Key);

        var sb = new StringBuilder();

        if (lead is null)
        {
            // Single track — byte-identical to the pre-lead output (design §7.4): \ts/\ks sit in the header,
            // and there is no \track wrapper.
            AppendHeader(sb, title, subtitle, tempo, ts, keySig, opts, state.ChordDefinitions);
            sb.Append(string.Join("\n", compingBars));
            return new RenderResult(sb.ToString(), state.Schedule);
        }

        // Two tracks (IN5): score metadata + the lone "." first, then a \track per staff carrying its own
        // bar metadata (\ts/\ks). The lead pattern renders as dead notes (\x.3); both tracks span the same
        // master bars so the staves stay aligned. Bars-per-row is a JS display setting (display.barsPerRow).
        List<string> leadBars = BuildLeadBars(song, rhythm, lead, ts, compingPlan);
        PrependTripletFeel(leadBars, tripletFeel);

        AppendScoreMetadata(sb, title, subtitle, tempo, opts, state.ChordDefinitions);
        AppendTrackHeader(sb, "Comping", "comp", ts, keySig);
        sb.Append(string.Join("\n", compingBars)).Append('\n');
        AppendTrackHeader(sb, "Lead", "lead", ts, keySig);
        sb.Append(string.Join("\n", leadBars));

        return new RenderResult(sb.ToString(), state.Schedule);
    }

    // The comping track body: a pickup measure (voiced with the first chord) then each section's bars, with
    // an inline \ks only when the section key changes. The threaded state carries the ":N" duration and the
    // active chord label across section seams, and collects \chord diagram definitions for the header.
    private List<string> BuildCompingBars(
        RealizedSong song, RhythmPattern rhythm, TimeSignature ts,
        RenderOptions opts, RenderState state)
    {
        IReadOnlyList<PatternBar> patternBars = PatternEventBars(rhythm);
        RealizedSection first = song.Sections[0];
        var barLines = new List<string>();

        // A pickup/anacrusis renders as a leading measure, voiced with the first chord of the first section
        // (ported from the old Render(Exercise) path — merge decision (a)).
        if (rhythm.Pickup is { } pickup && first.Bars.Count > 0)
        {
            RealizedSpan firstSpan = first.Bars[0].Spans[0];
            IReadOnlyList<RhythmSlot> pickupSlots = RhythmQuantizer.Quantize(pickup);
            // \ac marks the bar as an anacrusis: alphaTab sizes it by its actual beats (not the time
            // signature) instead of counting it as a full bar 1. It is bar metadata at the bar's start,
            // ahead of the ":N"/beats — so we prefix the already-correct short bar string (C2: still one bar).
            barLines.Add("\\ac " + RenderBar(pickupSlots, _ => firstSpan, first.Key, opts, state));
        }

        Key? previousKey = null;
        foreach (RealizedSection section in song.Sections)
        {
            EnsureMajorSupported(section.Key);

            // Inline \ks only when the key changes; the first section's key already sits in the header.
            if (previousKey is not null && !section.Key.Equals(previousKey))
            {
                barLines.Add("\\ks " + NoteSpeller.KeySignatureToken(section.Key));
            }

            RenderBars(section.Bars, patternBars, ts, section.Key, opts, state, barLines);
            previousKey = section.Key;
        }

        return barLines;
    }

    // The lead track body (v1 = dead notes): one bar per comping master bar so the two staves stay aligned.
    // The lead pattern tiles per section exactly as the comping pattern does; a comping pickup is mirrored as
    // a leading bar of rests (the lead doesn't play during the anacrusis in v1). Its own RenderState resets
    // the ":N" duration for this track (alphaTex duration state does not carry across tracks). Inline \ks on
    // a key change mirrors the comping walk so the master bars line up.
    private List<string> BuildLeadBars(
        RealizedSong song, RhythmPattern comping, RhythmPattern lead, TimeSignature ts, CompingPlan compingPlan)
    {
        IReadOnlyList<PatternBar> leadPatternBars = PatternEventBars(lead);
        RealizedSection first = song.Sections[0];
        var state = new RenderState { Plan = compingPlan };
        var barLines = new List<string>();

        if (comping.Pickup is { } pickup && first.Bars.Count > 0)
        {
            IReadOnlyList<RhythmSlot> pickupSlots = RhythmQuantizer.Quantize(pickup);
            // Mirror the comping pickup as a true anacrusis on the lead staff so both bars stay aligned.
            barLines.Add("\\ac " + RenderLeadBar(pickupSlots, state, allRests: true));
        }

        Key? previousKey = null;
        foreach (RealizedSection section in song.Sections)
        {
            if (previousKey is not null && !section.Key.Equals(previousKey))
            {
                barLines.Add("\\ks " + NoteSpeller.KeySignatureToken(section.Key));
            }

            for (int i = 0; i < section.Bars.Count; i++)
            {
                PatternBar patternBar = leadPatternBars[i % leadPatternBars.Count];
                IReadOnlyList<RhythmSlot> slots =
                    RhythmQuantizer.Quantize(patternBar.Events, ts, Array.Empty<int>(), patternBar.StartsTied);
                barLines.Add(RenderLeadBar(slots, state, allRests: false));
            }

            previousKey = section.Key;
        }

        return barLines;
    }

    private static void EnsureMajorSupported(Key key)
    {
        if (key.IsMinor)
        {
            throw new NotSupportedException("The MVP renderer supports major keys only.");
        }
    }

    // Single-track header: score metadata with \ts/\ks folded in before the lone "." (no \track wrapper).
    // Byte-identical to the pre-lead output (design §7.4).
    private static void AppendHeader(
        StringBuilder sb, string title, string subtitle, int tempo, TimeSignature ts, string keySig,
        RenderOptions options, IReadOnlyList<string> chordDefinitions)
    {
        sb.Append("\\title \"").Append(title).Append("\"\n");
        sb.Append("\\subtitle \"").Append(subtitle).Append("\"\n");
        sb.Append("\\tempo ").Append(tempo.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("\\ts ").Append(ts.Numerator).Append(' ').Append(ts.Denominator).Append('\n');
        sb.Append("\\ks ").Append(keySig).Append('\n');
        AppendChordDirectives(sb, options, chordDefinitions);
        sb.Append(".\n");
    }

    // Two-track score metadata: \ts/\ks move out to each \track (they are bar metadata), so the score block
    // is just title/subtitle/tempo + the chord directives, terminated by the lone ".".
    private static void AppendScoreMetadata(
        StringBuilder sb, string title, string subtitle, int tempo,
        RenderOptions options, IReadOnlyList<string> chordDefinitions)
    {
        sb.Append("\\title \"").Append(title).Append("\"\n");
        sb.Append("\\subtitle \"").Append(subtitle).Append("\"\n");
        sb.Append("\\tempo ").Append(tempo.ToString(CultureInfo.InvariantCulture)).Append('\n');
        AppendChordDirectives(sb, options, chordDefinitions);
        sb.Append(".\n");
    }

    // A \track line + its bar metadata (\ts/\ks). Bars-per-row is controlled on the JS side via
    // display.barsPerRow (works for single- AND multi-track); the old `{ defaultSystemsLayout 4 }` block was
    // multi-track-only and is no longer emitted.
    private static void AppendTrackHeader(
        StringBuilder sb, string name, string shortName, TimeSignature ts, string keySig)
    {
        sb.Append("\\track \"").Append(name).Append("\" \"").Append(shortName).Append("\"\n");
        sb.Append("\\ts ").Append(ts.Numerator).Append(' ').Append(ts.Denominator).Append('\n');
        sb.Append("\\ks ").Append(keySig).Append('\n');
    }

    // The chord-diagram directives, shared by the single-track header and the two-track score metadata.
    private static void AppendChordDirectives(
        StringBuilder sb, RenderOptions options, IReadOnlyList<string> chordDefinitions)
    {
        // Over-staff diagram visibility: \chordDiagramsInScore is the ONLY chord-diagram alphaTex metadata
        // directive (the top-of-score list is a stylesheet flag with no alphaTex directive — the JS render
        // component sets `globalDisplayChordDiagramsOnTop` for that). Emitted (bare = show, "false" = hide)
        // whenever a chord toggle is on; omitted otherwise so today's output stays byte-identical. Names
        // still show via {ch "…"}.
        if (options.ShowChordNames || options.ShowChordDiagramsOverStaff || options.ShowChordDiagramsOnTop)
        {
            sb.Append(options.ShowChordDiagramsOverStaff ? "\\chordDiagramsInScore\n" : "\\chordDiagramsInScore false\n");
        }

        // \chord definitions are header metadata (before the lone "."), one per distinct chord in first-use
        // order; the body references each by name with {ch "…"}. Needed by either diagram mode (the on-top
        // list renders the defined chords; over-staff renders them at each beat).
        if (options.ShowChordDiagramsOverStaff || options.ShowChordDiagramsOnTop)
        {
            foreach (string definition in chordDefinitions)
            {
                sb.Append(definition).Append('\n');
            }
        }
    }

    // The pattern's bars (timing only — swing is delegated to alphaTab's \tf, never warped here, C4). Each
    // PatternBar carries its events + its StartsTied flag (a leading '_' cross-bar tie). RenderBars /
    // BuildLeadBars tile these cyclically onto the progression.
    private static IReadOnlyList<PatternBar> PatternEventBars(RhythmPattern rhythm) => rhythm.Bars;

    // Prepend a single whole-song \tf to a track's first bar — bar metadata, ahead of the bar's :N/beats
    // and any \ac pickup prefix. None emits nothing, so a straight song is byte-identical to pre-\tf output.
    private static void PrependTripletFeel(List<string> bars, TripletFeel feel)
    {
        if (feel == TripletFeel.None || bars.Count == 0)
        {
            return;
        }

        bars[0] = $"\\tf {TripletFeelToken(feel)} " + bars[0];
    }

    // The alphaTex \tf ident for a feel (mirrors alphaTab's TripletFeel vocabulary).
    private static string TripletFeelToken(TripletFeel feel) => feel switch
    {
        TripletFeel.Triplet8th => "triplet8th",
        TripletFeel.Triplet16th => "triplet16th",
        TripletFeel.Dotted8th => "dotted8th",
        TripletFeel.Dotted16th => "dotted16th",
        TripletFeel.Scottish8th => "scottish8th",
        TripletFeel.Scottish16th => "scottish16th",
        _ => "none",
    };

    // Render a key-resolved run of bars, appending one line per bar. The per-bar logic (interior chord
    // boundaries → quantize → RenderBar) is shared verbatim by Render(Exercise) and Render(RealizedSong);
    // currentDuration is passed by ref so the stateful ":N" carries across both bars and section seams.
    // An m-bar pattern tiles cyclically: progression bar i uses pattern bar i % m (design §7 default; the
    // richer section-anchored alignment is owned by domain/multi-bar). Single-bar patterns (m=1) reduce to
    // the original "same bar everywhere" output.
    private void RenderBars(
        IReadOnlyList<RealizedBar> bars,
        IReadOnlyList<PatternBar> feltBars,
        TimeSignature ts,
        Key key,
        RenderOptions options,
        RenderState state,
        List<string> barLines)
    {
        for (int i = 0; i < bars.Count; i++)
        {
            RealizedBar bar = bars[i];
            PatternBar patternBar = feltBars[i % feltBars.Count];

            // Re-attack the strum at each interior chord change; quantize this bar against its own
            // boundaries so a slot landing on a new chord starts a fresh attack. A leading '_' (StartsTied)
            // ties the bar's first note to the previous bar's last (held via RenderState.LastVoicing).
            IReadOnlyList<int> boundaries = InteriorBoundaries(bar);
            IReadOnlyList<RhythmSlot> slots =
                RhythmQuantizer.Quantize(patternBar.Events, ts, boundaries, patternBar.StartsTied);
            barLines.Add(RenderBar(slots, bar.SpanCovering, key, options, state));
        }
    }

    // Bar-relative ticks where the chord changes (exclusive of 0 and the bar end). Empty for a
    // single-chord bar, so its quantization is identical to the pre-harmonic-rhythm output.
    private static IReadOnlyList<int> InteriorBoundaries(RealizedBar bar)
    {
        if (bar.Spans.Count <= 1)
        {
            return Array.Empty<int>();
        }

        var boundaries = new List<int>(bar.Spans.Count - 1);
        int tick = 0;
        for (int i = 0; i < bar.Spans.Count - 1; i++)
        {
            tick += bar.Spans[i].DurationTicks;
            boundaries.Add(tick);
        }

        return boundaries;
    }

    private string RenderBar(
        IReadOnlyList<RhythmSlot> slots,
        Func<int, RealizedSpan> spanForTick,
        Key key,
        RenderOptions options,
        RenderState state)
    {
        var tokens = new List<string>(slots.Count);
        int beatOrdinal = 0; // the slot's index within this bar — lines up with alphaTab's beat.index

        foreach (RhythmSlot slot in slots)
        {
            string durationToken = slot.NoteValue.ToString(CultureInfo.InvariantCulture);
            string prefix = string.Empty;
            if (durationToken != state.CurrentDuration)
            {
                prefix = ":" + durationToken + " ";
                state.CurrentDuration = durationToken;
            }

            // Beat effects collect into one brace group ({ch "…" d tu N}); the chord label is added only at a
            // chord change, the tuplet on every triplet-grid slot ({tu} does not persist like :N duration).
            var effects = new List<string>(2);
            string body;

            if (slot.TiedToPrevious)
            {
                // (b) rhythm wins over harmony: a tied note HOLDS the previously-sounding voicing's strings
                // (re-stated with the tie fret -.s), whatever chord the harmony moved to underneath — so the
                // chord change is silently overridden. No {ch} label and no schedule entry: it's not an attack.
                Voicing held = state.LastVoicing
                    ?? throw new InvalidOperationException("A tied note has no preceding voicing to hold.");
                body = TieNoteGroup(held);
            }
            else
            {
                // The span covering this slot's onset tick (harmonic-rhythm lookup) — to voice a sounding beat
                // (honoring a per-chord {…} override) and to detect a chord change for the schedule.
                RealizedSpan span = spanForTick(slot.StartTick);
                Chord chord = span.Chord;
                Voicing? voicing = null;

                if (slot.IsRest)
                {
                    body = "r";
                }
                else
                {
                    voicing = state.Plan.For(span);
                    state.LastVoicing = voicing;
                    body = NoteGroup(voicing);

                    bool wantsDiagram = options.ShowChordDiagramsOverStaff || options.ShowChordDiagramsOnTop;
                    if (options.ShowChordNames || wantsDiagram)
                    {
                        string name = ChordSymbol.Format(chord, key);
                        if (!string.Equals(name, state.CurrentChordName, StringComparison.Ordinal))
                        {
                            state.CurrentChordName = name;
                            // Collect each diagram definition once; it's emitted in the header, not inline.
                            if (wantsDiagram && state.DefinedChords.Add(name))
                            {
                                state.ChordDefinitions.Add(ChordDefinition(name, voicing));
                            }

                            effects.Add($"ch \"{name}\"");
                        }
                    }
                }

                RecordChordChange(state, span, key, beatOrdinal, voicing);
            }

            if (slot.Dotted)
            {
                effects.Add("d");
            }

            if (slot.Tuplet is { } tuplet)
            {
                effects.Add("tu " + tuplet.Numerator.ToString(CultureInfo.InvariantCulture));
            }

            string effectGroup = effects.Count > 0 ? "{" + string.Join(" ", effects) + "}" : string.Empty;
            tokens.Add(prefix + body + effectGroup);
            beatOrdinal++;
        }

        state.BarIndex++; // advance once per rendered bar (this is the comping walk; \ks/\tf lines aren't bars)
        return string.Join(" ", tokens) + " |";
    }

    // Push a schedule entry the first time a chord appears in playback order (the now/next-fretboards feed).
    // The diagram is built from the same voicing the tab comps for the chord (req C2): for a sounding beat
    // that voicing is already realized and reused; only a change landing on a rest realizes one here.
    private static void RecordChordChange(
        RenderState state, RealizedSpan span, Key key, int beatOrdinal, Voicing? voicing)
    {
        Chord chord = span.Chord;
        string name = ChordSymbol.Format(chord, key);
        if (string.Equals(name, state.ScheduleChordName, StringComparison.Ordinal))
        {
            return;
        }

        state.ScheduleChordName = name;
        state.Schedule.Add(new ChordChange(
            state.BarIndex, beatOrdinal, name,
            RealizedVoicingDiagram.Build(chord, voicing ?? state.Plan.For(span), key)));
    }

    // Render one lead-track bar (v1 = dead/muted notes): each hit is a dead note on string 3 (\x.3 —
    // rhythm only, no pitch; pitched LeadTargets are the deferred swap-in), each rest an "r". The stateful
    // ":N" duration and {tu N} tuplet tokens follow the same rules as the comping track. `allRests` renders
    // every slot as a rest (used to mirror the comping pickup so the staves stay bar-aligned).
    private static string RenderLeadBar(IReadOnlyList<RhythmSlot> slots, RenderState state, bool allRests)
    {
        var tokens = new List<string>(slots.Count);

        foreach (RhythmSlot slot in slots)
        {
            string durationToken = slot.NoteValue.ToString(CultureInfo.InvariantCulture);
            string prefix = string.Empty;
            if (durationToken != state.CurrentDuration)
            {
                prefix = ":" + durationToken + " ";
                state.CurrentDuration = durationToken;
            }

            // Dead-note lead: a tied continuation re-states string 3 with the tie fret (-.3).
            string body = allRests || slot.IsRest ? "r" : slot.TiedToPrevious ? "-.3" : "x.3";

            var effects = new List<string>(2);
            if (slot.Dotted)
            {
                effects.Add("d");
            }

            if (slot.Tuplet is { } tuplet)
            {
                effects.Add("tu " + tuplet.Numerator.ToString(CultureInfo.InvariantCulture));
            }

            string effectGroup = effects.Count > 0 ? "{" + string.Join(" ", effects) + "}" : string.Empty;
            tokens.Add(prefix + body + effectGroup);
        }

        return string.Join(" ", tokens) + " |";
    }

    private static string NoteGroup(Voicing voicing) =>
        "(" + string.Join(" ", voicing.Positions.Select(p => $"{p.Fret}.{p.String}")) + ")";

    // A tied continuation holding a voicing: each string re-stated with the alphaTex tie fret "-", so
    // alphaTab ties it to the prior note on that string (verified token, rhythm-notation chat). The held
    // voicing is the last attacked one, so a tie across a chord change keeps the previous chord (rhythm wins).
    private static string TieNoteGroup(Voicing voicing) =>
        "(" + string.Join(" ", voicing.Positions.Select(p => $"-.{p.String}")) + ")";

    // An alphaTex chord-diagram definition: \chord ("Name" f1 … f6), frets ordered string 1 (high E) →
    // string 6 (low E), an unplayed string written as x. The realized voicing carries the diagram hints.
    private static string ChordDefinition(string name, Voicing voicing)
    {
        var fretByString = voicing.Positions.ToDictionary(p => p.String, p => p.Fret);
        var frets = new List<string>(Fretboard.StringCount);
        for (int stringNumber = 1; stringNumber <= Fretboard.StringCount; stringNumber++)
        {
            frets.Add(fretByString.TryGetValue(stringNumber, out int fret)
                ? fret.ToString(CultureInfo.InvariantCulture)
                : "x");
        }

        // A grip up the neck needs {firstfret N} or alphaTab draws the box from the nut with the dots floating
        // in the air. FirstFret is 0/1 for open/nut grips (omit → default nut), ≥2 for a shifted grip.
        string firstFret = voicing.FirstFret is int ff && ff >= 2
            ? $" {{firstfret {ff.ToString(CultureInfo.InvariantCulture)}}}"
            : string.Empty;

        return $"\\chord (\"{name}\" {string.Join(" ", frets)}){firstFret}";
    }

    // Per-render mutable context (single-threaded over one Render call).
    private sealed class RenderState
    {
        // The resolved comping grips for this render (chord → voicing) — the renderer formats these (D4=(B)).
        public required CompingPlan Plan { get; init; }

        // The active alphaTex ":N" duration; persists across beats, bars, and section seams until changed.
        public string? CurrentDuration;

        // The last emitted chord label, so {ch "…"} is written only at a chord change.
        public string? CurrentChordName;

        // The voicing of the last sounding (attacked) note — a tied note re-states these strings, so a tie
        // holds the previous chord across a chord change (rhythm wins over harmony). Persists across bars.
        public Voicing? LastVoicing;

        // Chord labels whose \chord diagram has already been collected (define-once).
        public readonly HashSet<string> DefinedChords = new(StringComparer.Ordinal);

        // The \chord diagram definition lines, in first-use order — emitted in the header before the ".".
        public readonly List<string> ChordDefinitions = new();

        // --- Chord-schedule capture (the now/next-fretboards feed) ---
        // The master-bar index of the bar currently being rendered; advanced once per RenderBar (the comping
        // walk only — \ks/\tf lines are inline metadata, not bars, so they don't advance it).
        public int BarIndex;

        // The last chord label pushed to the schedule, so an entry is recorded only when the chord changes
        // (independent of the {ch "…"} option-gated label, which uses CurrentChordName).
        public string? ScheduleChordName;

        // One ChordChange per chord change, in playback order.
        public readonly List<ChordChange> Schedule = new();
    }
}

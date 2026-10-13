// Playable launch sequence (docs/MISSION_EVENTS.md §3, K5).
// The generator already compressed T- into game seconds. This clock walks those nominals:
// the whole kid count (about 32 s, inside the 20–40 s band), then every T+ callout as the
// live sim crosses it. Holds are announced by the poll, not by freezing the count.
// No Unity types. Same input ⇒ same beat list.
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Timeline
{
    public sealed class LaunchBeat
    {
        public string Id;
        public string Label;
        public string Banner;
        public string Phase;
        public double Nominal;
    }

    public sealed class LaunchSequence
    {
        readonly List<LaunchBeat> beats;
        readonly List<LaunchBeat> fired = new List<LaunchBeat>();
        int cursor;

        public string Mode { get; }
        public double Time { get; private set; }
        public double StartTime { get; }
        public bool Liftoff { get; private set; }
        public bool Held { get; private set; }
        public string Banner { get; private set; } = "Terminal count";
        public string Label { get; private set; } = "Terminal";
        public string Phase { get; private set; } = TimelinePhase.Terminal;
        public IReadOnlyList<LaunchBeat> Beats => beats;
        /// <summary>Every beat announced so far, including the one Open speaks at the opening of the count.</summary>
        public IReadOnlyList<LaunchBeat> Fired => fired;

        LaunchSequence(string mode, List<LaunchBeat> beats)
        {
            Mode = mode;
            this.beats = beats;
            StartTime = beats.Count > 0 ? beats[0].Nominal : 0;
            if (StartTime > 0) StartTime = 0;
            Time = StartTime;
            CatchUp(Time);
            if (Time >= 0) Liftoff = true;
        }

        /// <summary>Beats from the opening of the compressed count through the last planned callout.</summary>
        public static LaunchSequence Open(Timeline timeline)
        {
            if (timeline == null) throw new ArgumentNullException(nameof(timeline));
            string mode = TimelineClock.NormalizeMode(timeline.mode);
            var beats = new List<LaunchBeat>();
            if (timeline.events != null)
            {
                foreach (var e in timeline.events)
                {
                    if (e == null || string.IsNullOrEmpty(e.id)) continue;
                    if (e.status != null && e.status.state == TimelineStatus.Done) continue;
                    if (e.id.IndexOf(".hold.", StringComparison.Ordinal) >= 0) continue;
                    beats.Add(new LaunchBeat
                    {
                        Id = e.id,
                        Label = string.IsNullOrEmpty(e.shortLabel) ? e.id : e.shortLabel,
                        Banner = e.strings != null && !string.IsNullOrEmpty(e.strings.name) ? e.strings.name : e.name,
                        Phase = string.IsNullOrEmpty(e.phase) ? TimelinePhase.Terminal : e.phase,
                        Nominal = e.nominal,
                    });
                }
            }
            beats.Sort((a, b) =>
            {
                int c = a.Nominal.CompareTo(b.Nominal);
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });
            return new LaunchSequence(mode, beats);
        }

        public void Hold() => Held = true;
        public void Release() => Held = false;

        /// <summary>Advance the countdown toward T-0. Returns beats whose nominal was crossed, in order.</summary>
        public List<LaunchBeat> Step(double dt)
        {
            if (Liftoff || Held) return new List<LaunchBeat>();
            if (dt < 0) dt = 0;
            double next = Time + dt;
            if (next > 0) next = 0;
            Time = next;
            var fired = CatchUp(Time);
            if (Time >= 0) Liftoff = true;
            return fired;
        }

        /// <summary>
        /// After liftoff, fire every planned callout whose nominal is at or before <paramref name="simTime"/>.
        /// Nominals come from the same ascent sim, so the board and the vehicle agree.
        /// </summary>
        public List<LaunchBeat> AdvanceSim(double simTime)
        {
            if (!Liftoff || Held) return new List<LaunchBeat>();
            if (simTime < 0) simTime = 0;
            return CatchUp(simTime);
        }

        public string Clock => TimelineClock.Clock(Time);

        List<LaunchBeat> CatchUp(double t)
        {
            var announced = new List<LaunchBeat>();
            while (cursor < beats.Count && beats[cursor].Nominal <= t + 1e-9)
            {
                var b = beats[cursor++];
                Banner = string.IsNullOrEmpty(b.Banner) ? b.Label : b.Banner;
                Label = b.Label;
                Phase = b.Phase;
                fired.Add(b);
                announced.Add(b);
            }
            return announced;
        }
    }
}

// Live flight board, laid out like a launch webcast: a short run of mission events, the one
// happening now, and a plain reading of how this design is doing against the trip it was built for.
// No Unity types. The same time and the same sheet always produce the same chips.
using System;
using System.Collections.Generic;
using Kestrel.Core.Timeline;

namespace Kestrel.Core.Flight
{
    public enum ChipState { Past, Now, Next }

    public sealed class WebcastChip
    {
        public string Id;
        public string Label;
        public string Clock;
        public ChipState State;
        public int Index;
    }

    public sealed class DesignPulse
    {
        public string Verdict;
        public string Detail;
        /// <summary>Speed so far divided by the mission's required speed. Above 1 means the speed is there.</summary>
        public double SpeedFraction;
        /// <summary>Live dynamic pressure divided by the design's max-Q readout. 0 when that readout is missing.</summary>
        public double HeatFraction;
    }

    public static class WebcastBoard
    {
        public const int Behind = 1;
        public const int Ahead = 3;
        public const double HeatWarn = 0.85;

        /// <summary>Index of the latest beat whose nominal is at or before <paramref name="time"/>. -1 before the first.</summary>
        public static int CurrentIndex(IReadOnlyList<LaunchBeat> beats, double time)
        {
            int cur = -1;
            if (beats == null) return cur;
            for (int i = 0; i < beats.Count; i++)
                if (beats[i] != null && beats[i].Nominal <= time + 1e-9) cur = i;
            return cur;
        }

        /// <summary>The webcast strip: one beat behind, the current beat, and the next few. Empty when there is no sequence.</summary>
        public static List<WebcastChip> Window(IReadOnlyList<LaunchBeat> beats, double time)
        {
            var chips = new List<WebcastChip>();
            if (beats == null || beats.Count == 0) return chips;
            int cur = CurrentIndex(beats, time);
            int from = cur < 0 ? 0 : Math.Max(0, cur - Behind);
            int to = Math.Min(beats.Count - 1, (cur < 0 ? -1 : cur) + Ahead);
            for (int i = from; i <= to; i++)
            {
                var b = beats[i];
                if (b == null) continue;
                chips.Add(new WebcastChip
                {
                    Id = b.Id,
                    Label = string.IsNullOrEmpty(b.Label) ? b.Id : b.Label,
                    Clock = TimelineClock.Clock(b.Nominal),
                    State = i < cur ? ChipState.Past : i == cur ? ChipState.Now : ChipState.Next,
                    Index = i,
                });
            }
            return chips;
        }

        /// <summary>
        /// How the flying design compares with the speed the mission asked for, and with its own max-Q readout.
        /// Before liftoff the verdict is the count, and the detail is the sheet's spare or shortfall.
        /// </summary>
        public static DesignPulse Pulse(bool flying, double altitudeM, double speedMps, double requiredMps, double spareMps, double qPa, double? maxQKpa, string orbitLabel)
        {
            double req = requiredMps > 1 ? requiredMps : 1;
            double frac = flying ? speedMps / req : 0;
            double heat = 0;
            if (maxQKpa.HasValue && maxQKpa.Value > 0.1)
                heat = (qPa / 1000.0) / maxQKpa.Value;
            string trip = string.IsNullOrEmpty(orbitLabel) ? "this trip" : orbitLabel;
            var pulse = new DesignPulse { SpeedFraction = frac, HeatFraction = heat };
            if (!flying)
            {
                pulse.Verdict = "On the count";
                pulse.Detail = spareMps >= 0
                    ? $"{spareMps:N0} m/s to spare for {trip}"
                    : $"{-spareMps:N0} m/s short for {trip}";
                return pulse;
            }
            bool heating = heat >= HeatWarn;
            if (heating) pulse.Verdict = "Heating hard";
            else if (frac >= 1) pulse.Verdict = "Speed is there";
            else if (altitudeM < 10000) pulse.Verdict = "Climbing";
            else if (frac < 0.40) pulse.Verdict = "Short on speed";
            else pulse.Verdict = "On track";
            string q = maxQKpa.HasValue ? $" · Q {qPa / 1000:0} of {maxQKpa.Value:0} kPa" : $" · Q {qPa / 1000:0} kPa";
            pulse.Detail = $"{speedMps / 1000:0.00} of {req / 1000:0.00} km/s{q}";
            return pulse;
        }

        /// <summary>The stack has come apart. The verdict stays on the board for the rest of the flight.</summary>
        public static DesignPulse Lost(string kind, double altitudeM)
        {
            string where = $"{altitudeM / 1000:0.0} km";
            string detail;
            switch (kind)
            {
                case "structural-maxq": detail = "The stack broke apart at " + where + "."; break;
                case "overg": detail = "The push snapped the stack at " + where + "."; break;
                case "tumble": detail = "It tumbled and came apart at " + where + "."; break;
                default: detail = "The vehicle came apart at " + where + "."; break;
            }
            return new DesignPulse { Verdict = "Vehicle lost", Detail = detail };
        }

        /// <summary>One line for a tapped event, using the live numbers. The same beat and the same pulse always read the same.</summary>
        public static string Callout(LaunchBeat beat, DesignPulse pulse, double altitudeM)
        {
            if (beat == null) return pulse != null ? pulse.Detail : "";
            string id = beat.Id ?? "";
            string name = string.IsNullOrEmpty(beat.Banner) ? beat.Label : beat.Banner;
            string alt = $"{altitudeM / 1000:0.0} km";
            if (id.IndexOf("maxq", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("max-q", StringComparison.OrdinalIgnoreCase) >= 0)
                return name + ". Thickest air on the way up. " + (pulse != null ? pulse.Detail : "");
            if (id.IndexOf("karman", StringComparison.OrdinalIgnoreCase) >= 0)
                return name + ". The edge of space is 100 km. You are at " + alt + ".";
            if (id.IndexOf("fairing", StringComparison.OrdinalIgnoreCase) >= 0)
                return name + ". The nose cone waits until the air is thin. You are at " + alt + ".";
            if (id.IndexOf("meco", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("seco", StringComparison.OrdinalIgnoreCase) >= 0)
                return name + ". Engines stop. " + (pulse != null ? pulse.Detail : "");
            if (id.IndexOf("liftoff", StringComparison.OrdinalIgnoreCase) >= 0)
                return name + ". The pad lets go.";
            if (beat.Nominal < 0)
                return name + " at " + TimelineClock.Clock(beat.Nominal) + ".";
            return name + ". " + (pulse != null ? pulse.Verdict : "");
        }
    }
}

// Pre-launch success prediction. One headless sim, then the truth of that sim.
// The ascent event list is the timeline. There is no noise model, so a second seed
// repeats the same flight and the probability is 0 or 1. KID reads the pass/fail meter.
using System;
using System.Globalization;
using System.Text;
using Kestrel.Core.Data;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public sealed class LaunchPrediction
    {
        public string VehicleId;
        public string Mode;
        public uint Seed;
        public int Runs;
        public string Outcome;
        public bool Success;
        public double Probability;
        public string Meter;
        public double ApoKm;
        public double PeriKm;
        public double TargetKm;
        public double DvMarginMs;
        public string Timeline;
        public string Text;

        public string Canonical()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("vehicle=").Append(VehicleId).Append('\n');
            sb.Append("mode=").Append(Mode).Append('\n');
            sb.Append("seed=").Append(Seed.ToString(inv)).Append('\n');
            sb.Append("runs=").Append(Runs.ToString(inv)).Append('\n');
            sb.Append("outcome=").Append(Outcome).Append('\n');
            sb.Append("success=").Append(Success ? "true" : "false").Append('\n');
            sb.Append("probability=").Append(Probability.ToString("0", inv)).Append('\n');
            sb.Append("meter=").Append(Meter).Append('\n');
            sb.Append("apoKm=").Append(ApoKm.ToString("0.0", inv)).Append('\n');
            sb.Append("periKm=").Append(PeriKm.ToString("0.0", inv)).Append('\n');
            sb.Append("targetKm=").Append(TargetKm.ToString("0.0", inv)).Append('\n');
            sb.Append("dvMarginMs=").Append(DvMarginMs.ToString("0", inv)).Append('\n');
            sb.Append("timeline=").Append(Timeline).Append('\n');
            sb.Append("text=").Append(Text).Append('\n');
            return sb.ToString();
        }
    }

    public static class LaunchPredictor
    {
        public static LaunchPrediction Predict(Catalog catalog, Design vehicle, string mode, uint seed)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
            mode = string.Equals(mode, "kid", StringComparison.OrdinalIgnoreCase) ? "kid" : "standard";
            var input = Stack.SimInputs(catalog, vehicle);
            var sim = Headless.Simulate(input.stages, input.opts);
            var req = Stack.RequiredDeltaV(catalog, vehicle.payloadOrbit);
            bool suborbital = req.orbit != null && req.orbit.periKm < 0;
            // The sim raises "orbit" inside a 10 km cutoff band. The prediction uses the
            // target itself: perigee at or above the orbital target, apogee for a suborbital hop.
            bool success = suborbital ? sim.orbit.apoAlt >= req.targetAlt : sim.orbit.periAlt >= req.targetAlt;
            double apoKm = sim.orbit.apoAlt / 1000.0;
            double periKm = sim.orbit.periAlt / 1000.0;
            double targetKm = req.targetAlt / 1000.0;
            double dvMargin = sim.idealDeltaV - req.required;
            var timeline = new StringBuilder();
            if (sim.events != null)
            {
                for (int i = 0; i < sim.events.Count; i++)
                {
                    if (i > 0) timeline.Append(',');
                    timeline.Append(sim.events[i].type);
                }
            }
            var p = new LaunchPrediction
            {
                VehicleId = vehicle.id,
                Mode = mode,
                Seed = seed,
                Runs = 1,
                Outcome = sim.outcome,
                Success = success,
                Probability = success ? 1 : 0,
                Meter = success ? "pass" : "fail",
                ApoKm = FloorStep(apoKm, 0.1),
                PeriKm = FloorStep(periKm, 0.1),
                TargetKm = FloorStep(targetKm, 0.1),
                DvMarginMs = dvMargin >= 0 ? FloorStep(dvMargin, 1) : -CeilStep(-dvMargin, 1),
                Timeline = timeline.ToString(),
            };
            p.Text = Sentence(p, mode, suborbital, sim.outcome == "orbit");
            return p;
        }

        public static LaunchPrediction Predict(Catalog catalog, Design vehicle)
            => Predict(catalog, vehicle, "standard", 1);

        static string Sentence(LaunchPrediction p, string mode, bool suborbital, bool simOrbitFlag)
        {
            var inv = CultureInfo.InvariantCulture;
            string apo = p.ApoKm.ToString("0.0", inv);
            string peri = p.PeriKm.ToString("0.0", inv);
            string target = p.TargetKm.ToString("0.0", inv);
            string height = suborbital ? "Apogee " + apo + " km" : "Perigee " + peri + " km, apogee " + apo + " km";
            if (mode == "kid")
                return (p.Success ? "Pass. " : "Fail. ") + (suborbital ? "Apogee " + apo : "Perigee " + peri) + " km.";
            if (p.Success)
                return "The sim reaches the target. " + height + ", target " + target + " km. Ideal Δv margin " + p.DvMarginMs.ToString("0", inv) + " m/s.";
            string gap = p.DvMarginMs < 0
                ? " Short by " + (-p.DvMarginMs).ToString("0", inv) + " m/s Δv."
                : " Ideal Δv still shows " + p.DvMarginMs.ToString("0", inv) + " m/s of margin; the flight does not.";
            string flag = !suborbital && simOrbitFlag ? " The ascent sim still raises an orbit flag; the target is the perigee." : "";
            return "The sim does not reach the target. " + height + ", target " + target + " km." + gap + flag;
        }

        static double FloorStep(double value, double step) => Math.Floor(value / step + 1e-9) * step;

        static double CeilStep(double value, double step)
        {
            if (value <= 0) return 0;
            return Math.Ceiling(value / step - 1e-9) * step;
        }
    }
}

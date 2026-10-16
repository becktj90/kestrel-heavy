// Landing reserve rides as unburned mass, so the same ascent Δv fits less payload.
// The cost is the extra cargo a zero-reserve copy can carry before its Δv falls back
// to the reserved rocket. Expendable with no reserve costs nothing.
using System;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public sealed class RecoveryTradeoff
    {
        public double ReserveKg, PayloadCostKg, DvWithMps, DvWithoutMps;
        public string Mode, Kid, Engineer;

        public static RecoveryTradeoff None()
        {
            return new RecoveryTradeoff
            {
                Mode = "expendable",
                Kid = "No landing fuel held back. The whole load can fly.",
                Engineer = "Reserve 0 kg. Payload cost 0 kg.",
            };
        }

        public static RecoveryTradeoff Measure(Catalog catalog, Design design)
        {
            if (catalog == null || design == null || design.stages == null || design.stages.Count == 0)
                return None();
            double reserve = design.stages[0].landingPropMass;
            if (double.IsNaN(reserve) || reserve < 0) reserve = 0;
            string mode = string.IsNullOrEmpty(design.recoveryMode) ? "expendable" : design.recoveryMode;
            double with = DeltaV(catalog, design);
            if (reserve <= 0.5)
            {
                var empty = None();
                empty.Mode = mode;
                empty.DvWithMps = with;
                empty.DvWithoutMps = with;
                if (mode == "expendable")
                {
                    empty.Kid = "Expendable. No fuel held for Haven.";
                    empty.Engineer = "Recovery expendable. Reserve 0 kg. Ascent Δv " + Fmt(with) + " m/s.";
                }
                return empty;
            }

            var free = design.Clone();
            free.stages[0].landingPropMass = 0;
            double bare = DeltaV(catalog, free);
            double lo = 0, hi = 250000;
            for (int i = 0; i < 28; i++)
            {
                double mid = (lo + hi) * 0.5;
                var trial = free.Clone();
                trial.payloadMass = design.payloadMass + mid;
                double dv = DeltaV(catalog, trial);
                if (dv >= with) lo = mid;
                else hi = mid;
            }
            var inv = CultureInfo.InvariantCulture;
            string kg = Math.Round(reserve).ToString("0", inv);
            string cost = Math.Round(lo).ToString("0", inv);
            return new RecoveryTradeoff
            {
                ReserveKg = reserve,
                PayloadCostKg = lo,
                DvWithMps = with,
                DvWithoutMps = bare,
                Mode = mode,
                Kid = "Holding " + kg + " kg for landing costs " + cost + " kg of cargo.",
                Engineer = "Reserve " + reserve.ToString("0", inv) + " kg. Ascent Δv " + Fmt(with)
                    + " m/s with it, " + Fmt(bare) + " m/s without. Same Δv fits "
                    + lo.ToString("0", inv) + " kg less payload.",
            };
        }

        static double DeltaV(Catalog catalog, Design design)
        {
            var serial = Stack.SerialStages(catalog, design);
            var sims = serial.stages.ConvertAll(s => s.ToSim());
            return Headless.StackDeltaV(sims, serial.payload).total;
        }

        static string Fmt(double mps) => Math.Round(mps).ToString("0", CultureInfo.InvariantCulture);
    }
}

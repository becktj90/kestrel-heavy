// Closed-form drag samples from tools/golden-flight.mjs (PR #31). The peak-q sample is recomputed;
// the stored sample.q is not the expected drag.
using System;
using System.Collections.Generic;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public struct DragSample
    {
        public double t, alt, vrel, dragForce, dynamicPressure, cd;
        public int stage;
    }

    public struct MachPeak
    {
        public double gridAltitudeM, mach, dragForce, dynamicPressure, cd;
    }

    public static class CatalogDrag
    {
        public static readonly double[] AltitudeGridM = { -500, 0, 1000, 5000, 11000, 15000, 20000, 25000, 32000, 40000, 47000, 51000, 60000, 71000, 80000, 84852, 86000, 90000, 100000, 150000, 250000 };
        public const double CatalogFlightDt = 0.2;

        public static DragSample Sample(IList<SimSample> samples, IList<SimStage> stages)
        {
            SimSample peak = samples[0];
            for (int i = 1; i < samples.Count; i++) if (samples[i].q > peak.q) peak = samples[i];
            double alt = Math.Max(0, peak.alt);
            var atm = Atmosphere.At(alt);
            int idx = Math.Min(Math.Max(peak.stage, 1), stages.Count) - 1;
            var stage = stages[idx];
            double area = Aero.RefArea(stage.diameter);
            double vrel = peak.vrel;
            return new DragSample
            {
                t = peak.t, alt = peak.alt, vrel = vrel, stage = peak.stage,
                dragForce = Aero.DragForce(atm.rho, vrel, atm.a, stage.cd, area),
                dynamicPressure = Aero.DynamicPressure(atm.rho, vrel),
                cd = Aero.CdMach(atm.a > 0 ? vrel / atm.a : 0, stage.cd),
            };
        }

        public static MachPeak Peak(double cd0, double area)
        {
            MachPeak best = default;
            bool any = false;
            foreach (double z in AltitudeGridM)
            {
                if (!(z >= 0)) continue;
                var atm = Atmosphere.At(z);
                double v = atm.a * Aero.M_PEAK;
                double q = Aero.DynamicPressure(atm.rho, v);
                if (!any || q > best.dynamicPressure)
                {
                    any = true;
                    best = new MachPeak
                    {
                        gridAltitudeM = z, mach = Aero.M_PEAK,
                        dragForce = Aero.DragForce(atm.rho, v, atm.a, cd0, area),
                        dynamicPressure = q, cd = Aero.CdMach(Aero.M_PEAK, cd0),
                    };
                }
            }
            return best;
        }
    }
}

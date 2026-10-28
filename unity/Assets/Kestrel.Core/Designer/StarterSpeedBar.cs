// The Kid "Speed to reach orbit" bar on the Starter screen (docs/DESIGNER_TIERS_SPEC.md, Kid readouts): the rocket's
// delta-v (Readout.Quick through StarterRocket.Check) against the mission's need, with a need marker and an
// "Enough" / "Short" word. Hops (KH-1, KH-2) measure against the rocket that flew the hop, exactly as the readiness line
// does (StarterScreen.HopSame), because the orbital margin reads short on rockets that fly the hop (M-10).
// Information only: it never gates Launch and never touches the badge (LaunchGate / readiness own those).
using System;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Designer
{
    public sealed class StarterSpeed
    {
        public bool Visible;
        public string Label = "", Status = "", Spoken = "";
        public bool Enough;
        /// <summary>m/s: what the rocket has and what the mission needs.</summary>
        public double Have, Need;
        /// <summary>0..1 along the bar: the fill and the need marker.</summary>
        public double Fill, NeedAt;
    }

    public static class StarterSpeedBar
    {
        /// <summary>The bar spans this much past the need, so the marker sits left of the end and "plenty" still shows room.</summary>
        public const double Headroom = 1.25;

        public static StarterSpeed Of(StarterRocket r, DesignerTierText text)
        {
            var o = new StarterSpeed();
            if (r == null || text == null) return o;
            var c = r.Check();
            if (c == null || c.Blocked || !c.HasNumbers || !(c.DeltaV > 0)) return o;
            var m = r.Mission;
            double need;
            if (m != null && m.TargetKind == "apogee")
            {
                var b = r.BaseCheck();
                if (b == null || !b.HasNumbers || !(b.DeltaV > 0)) return o;
                need = b.DeltaV * (1 - StarterScreen.HopSame);
                o.Label = text.SpeedHop;
            }
            else
            {
                need = c.Required;
                o.Label = m != null && m.TargetKind == "c3" ? text.SpeedEscape : text.SpeedOrbit;
            }
            if (!(need > 0) || double.IsNaN(need) || double.IsInfinity(need)) return o;
            o.Have = c.DeltaV;
            o.Need = need;
            o.Enough = o.Have >= need;
            double span = Math.Max(need * Headroom, o.Have);
            o.Fill = Clamp01(o.Have / span);
            o.NeedAt = Clamp01(need / span);
            o.Status = o.Enough ? text.SpeedEnough : text.SpeedShort;
            o.Spoken = (o.Enough ? text.SpokenEnough : text.SpokenShort).Replace("{label}", o.Label);
            o.Visible = !string.IsNullOrEmpty(o.Label) && !string.IsNullOrEmpty(o.Status);
            return o;
        }

        static double Clamp01(double v) => double.IsNaN(v) ? 0 : v < 0 ? 0 : v > 1 ? 1 : v;
    }
}

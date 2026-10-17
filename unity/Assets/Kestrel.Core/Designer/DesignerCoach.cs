// Plain-language coach for the designer. The sentences quote the Codex kid lines and the live readout.
// They do not add numbers the flight model does not already publish.
using System.Globalization;
using Kestrel.Core.Codex;

namespace Kestrel.Core.Designer
{
    public static class DesignerCoach
    {
        public static readonly string[] Flow = { "Mission", "Engines", "Tanks", "Stages", "Landing", "Look", "Launch" };
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Goal(string step)
        {
            switch (step)
            {
                case "Mission": return "Pick the job. What are we carrying, and where is it going?";
                case "Engines": return "Give it enough push to leave the pad.";
                case "Tanks": return "Carry the fuel this trip needs.";
                case "Stages": return "Staging drops empty tanks so the rocket left flying is lighter.";
                case "Landing": return "Decide where the booster comes down, and save fuel for it.";
                case "Look": return "Paint it. The paint does not change how it flies.";
                case "Launch": return "Check the speed you have against the speed this trip needs, then light it.";
                default: return "";
            }
        }

        public static string CodexId(string concept)
        {
            switch (concept)
            {
                case "mission":
                case "orbit": return "orbits";
                case "engines":
                case "twr": return "twr";
                case "isp": return "isp";
                case "tanks":
                case "prop": return "delta-v";
                case "stages": return "staging";
                case "max-q": return "max-q";
                case "landing": return "booster-landing";
                case "stability": return "cg-cp";
                default: return null;
            }
        }

        public static string BudgetLine(double haveMps, double needMps)
        {
            string have = N0(haveMps);
            string need = N0(needMps);
            if (haveMps + 1 >= needMps)
                return "Have " + have + " m/s. Need " + need + " m/s. " + N0(haveMps - needMps) + " m/s to spare.";
            return "Have " + have + " m/s. Need " + need + " m/s. " + N0(needMps - haveMps) + " m/s short.";
        }

        public static string WhyMission(string orbit, double needMps)
        {
            string where = string.IsNullOrEmpty(orbit) ? "this trip" : orbit;
            return "This trip needs " + N0(needMps) + " m/s to reach " + where + ". A heavier payload needs more speed. A different orbit changes the need.";
        }

        public static string WhyEngines(double twr, double ispVac, double thrustVacN)
        {
            string s = "TWR is " + twr.ToString("0.00", Inv) + ". Above 1, the stack leaves the pad. At 1.25 or more, the climb wastes less speed on gravity. ";
            if (ispVac > 0)
                s += "Vacuum Isp is " + ispVac.ToString("0", Inv) + " s. Higher Isp means more speed from each kilogram. ";
            if (thrustVacN > 0)
                s += "Vacuum thrust is " + N0(thrustVacN) + " N. ";
            s += "More engines raise the push. Fewer engines lower it.";
            return s;
        }

        public static string WhyTanks(double haveMps, double needMps)
            => "Have " + N0(haveMps) + " m/s. Need " + N0(needMps) + " m/s. More fuel adds speed and weight. Less fuel does the opposite.";

        public static string WhyStages(int stages, double haveMps)
            => stages + (stages == 1 ? " stage. " : " stages. ")
                + "Staging drops empty tanks so the rocket left flying is lighter. Each stage adds its own delta-v. This stack has " + N0(haveMps) + " m/s.";

        public static string WhyMaxQ(double? peakKpa, double? limitKpa)
        {
            string peak = peakKpa.HasValue ? peakKpa.Value.ToString("0.0", Inv) + " kPa" : "not flown yet";
            string lim = limitKpa.HasValue ? limitKpa.Value.ToString("0.0", Inv) + " kPa" : "unknown";
            return "Peak air load is " + peak + ". The walls hold " + lim + ". The designer warns above 45 kPa. That warning does not block the launch. A higher safety factor raises the limit. A lower max-Q throttle eases the push.";
        }

        public static string WhyGLimit(double gLimit)
            => "The payload is rated " + gLimit.ToString("0.0", Inv) + " g. A lower g limit tells the autopilot to throttle back before the stack reaches it. Raising the limit lets the push climb higher.";

        public static string WhyLanding(double haveMps, double needMps)
            => "The landing reserve does not count toward orbit. Have " + N0(haveMps) + " m/s. Need " + N0(needMps) + " m/s. Saving fuel for the way down leaves less speed for the trip.";

        public static string WhyStability(double stabilityCal)
            => "Stability is " + stabilityCal.ToString("0.00", Inv) + " calibers. The center of mass is where the rocket balances. The center of pressure is where the air pushes. Fins pull the push point back. Aim for 1 to 2 body widths of gap.";

        public static string CodexBlurb(CodexBook book, string id, bool engineer)
        {
            if (book == null || string.IsNullOrEmpty(id)) return "The Codex note is not loaded.";
            var entry = book.ById(id);
            if (entry == null) return "The Codex note is not loaded.";
            if (!engineer || string.IsNullOrEmpty(entry.Engineer)) return entry.Kid;
            return entry.Kid + "\n" + entry.Engineer;
        }

        static string N0(double x) => x.ToString("N0", Inv);
    }
}

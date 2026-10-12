// Headless command surface for the dotnet CLI. mc is named and refused until P8.
// propagate runs Kepler or on-rails coast (P5). simulate runs the built-in medium lifter.
// atmo is Atmosphere.At.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Cli
{
    public static class Commands
    {
        public static int Run(string[] args, out string stdout)
        {
            var inv = CultureInfo.InvariantCulture;
            if (args == null || args.Length == 0) { stdout = Usage(); return 2; }
            switch (args[0])
            {
                case "atmo":
                    if (args.Length != 2 || !double.TryParse(args[1], NumberStyles.Float, inv, out double z))
                    { stdout = Usage(); return 2; }
                    var s = Atmosphere.At(z);
                    stdout = "{\"z\":" + z.ToString("R", inv) + ",\"T\":" + s.T.ToString("R", inv) + ",\"p\":" + s.p.ToString("R", inv) + ",\"rho\":" + s.rho.ToString("R", inv) + ",\"a\":" + s.a.ToString("R", inv) + "}";
                    return 0;
                case "era":
                    if (args.Length != 5
                        || !int.TryParse(args[1], NumberStyles.Integer, inv, out int y)
                        || !int.TryParse(args[2], NumberStyles.Integer, inv, out int mo)
                        || !int.TryParse(args[3], NumberStyles.Integer, inv, out int d)
                        || !double.TryParse(args[4], NumberStyles.Float, inv, out double hours))
                    { stdout = Usage(); return 2; }
                    Frames.JdUt1Parts(y, mo, d, hours, out double jd1, out double jd2);
                    stdout = "{\"jd1\":" + jd1.ToString("R", inv) + ",\"jd2\":" + jd2.ToString("R", inv) + ",\"eraRad\":" + Frames.EraRad(jd1, jd2).ToString("R", inv) + "}";
                    return 0;
                case "simulate":
                    var r = Headless.Simulate(Headless.MediumLifter(), new SimOptions { dt = 1, tMax = 2000 });
                    stdout = "{\"outcome\":\"" + r.outcome + "\",\"time\":" + r.time.ToString("R", inv) + "}";
                    return 0;
                case "propagate":
                    return Propagate(args, inv, out stdout);
                case "readout-bench":
                    return ReadoutBenchCommand(out stdout);
                case "predict":
                case "mc":
                    stdout = "{\"command\":\"" + args[0] + "\",\"status\":\"not-implemented\"}";
                    return 0;
                default:
                    stdout = Usage();
                    return 2;
            }
        }

        static int ReadoutBenchCommand(out string stdout)
        {
            string root;
            try { root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory()); }
            catch (Exception e) { stdout = e.Message; return 2; }
            var cat = Catalog.LoadFromRepo(root);
            var designs = ReadoutBench.LoadDesigns(root);
            var r = ReadoutBench.Measure(cat, designs);
            var inv = CultureInfo.InvariantCulture;
            stdout = "{\"designs\":" + r.designs.ToString(inv)
                + ",\"quickP95Ms\":" + r.quickP95Ms.ToString("R", inv)
                + ",\"validateP95Ms\":" + r.validateP95Ms.ToString("R", inv)
                + ",\"referenceMaxMs\":" + r.referenceMaxMs.ToString("R", inv)
                + ",\"quickWorst\":\"" + r.quickWorst + "\""
                + ",\"validateWorst\":\"" + r.validateWorst + "\""
                + ",\"referenceWorst\":\"" + r.referenceWorst + "\""
                + ",\"quickBudgetMs\":" + ReadoutBench.QuickP95BudgetMs.ToString("R", inv)
                + ",\"referenceBudgetMs\":" + ReadoutBench.ReferenceBudgetMs.ToString("R", inv)
                + ",\"ok\":" + (r.ok ? "true" : "false") + "}";
            return r.ok ? 0 : 1;
        }

        static int Propagate(string[] args, CultureInfo inv, out string stdout)
        {
            // propagate <kepler|onrails|auto> rx ry rz vx vy vz dt
            if (args.Length != 9)
            { stdout = Usage(); return 2; }
            string mode = args[1].ToLowerInvariant();
            if (mode != "kepler" && mode != "onrails" && mode != "auto")
            { stdout = Usage(); return 2; }
            if (!double.TryParse(args[2], NumberStyles.Float, inv, out double rx0)
                || !double.TryParse(args[3], NumberStyles.Float, inv, out double ry0)
                || !double.TryParse(args[4], NumberStyles.Float, inv, out double rz0)
                || !double.TryParse(args[5], NumberStyles.Float, inv, out double vx0)
                || !double.TryParse(args[6], NumberStyles.Float, inv, out double vy0)
                || !double.TryParse(args[7], NumberStyles.Float, inv, out double vz0)
                || !double.TryParse(args[8], NumberStyles.Float, inv, out double dt))
            { stdout = Usage(); return 2; }

            CoastMode? overrideMode = null;
            if (mode == "kepler") overrideMode = CoastMode.Numeric;
            else if (mode == "onrails") overrideMode = CoastMode.OnRails;

            Handoff.PropagateCoast(rx0, ry0, rz0, vx0, vy0, vz0, dt,
                out double rx, out double ry, out double rz, out double vx, out double vy, out double vz,
                overrideMode);
            string used = mode == "auto"
                ? (Handoff.SelectCoast(rx0, ry0, rz0, vx0, vy0, vz0) == CoastMode.OnRails ? "onrails" : "kepler")
                : mode;
            stdout = "{\"mode\":\"" + used + "\",\"rx\":" + rx.ToString("R", inv)
                + ",\"ry\":" + ry.ToString("R", inv) + ",\"rz\":" + rz.ToString("R", inv)
                + ",\"vx\":" + vx.ToString("R", inv) + ",\"vy\":" + vy.ToString("R", inv)
                + ",\"vz\":" + vz.ToString("R", inv) + "}";
            return 0;
        }

        static string Usage()
        {
            var sb = new StringBuilder();
            sb.Append("kestrel atmo <zMetres> | era <year> <month> <day> <hoursUtc> | simulate | predict | mc | readout-bench | propagate <kepler|onrails|auto> <rx> <ry> <rz> <vx> <vy> <vz> <dt>");
            return sb.ToString();
        }
    }
}

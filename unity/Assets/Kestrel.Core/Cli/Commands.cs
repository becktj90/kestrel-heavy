// Headless command surface for the dotnet CLI. mc and propagate are named and refused
// until P8 and P5. simulate runs the built-in medium lifter. atmo is Atmosphere.At.
using System;
using System.Globalization;
using System.Text;
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
                    double jd = Frames.JdUt1FromUtc(y, mo, d, hours);
                    stdout = "{\"jdUt1\":" + jd.ToString("R", inv) + ",\"eraRad\":" + Frames.EraRad(jd).ToString("R", inv) + "}";
                    return 0;
                case "simulate":
                    var r = Headless.Simulate(Headless.MediumLifter(), new SimOptions { dt = 1, tMax = 2000 });
                    stdout = "{\"outcome\":\"" + r.outcome + "\",\"time\":" + r.time.ToString("R", inv) + "}";
                    return 0;
                case "predict":
                case "mc":
                case "propagate":
                    stdout = "{\"command\":\"" + args[0] + "\",\"status\":\"not-implemented\"}";
                    return 0;
                default:
                    stdout = Usage();
                    return 2;
            }
        }

        static string Usage()
        {
            var sb = new StringBuilder();
            sb.Append("kestrel atmo <zMetres> | era <year> <month> <day> <hoursUtc> | simulate | predict | mc | propagate");
            return sb.ToString();
        }
    }
}

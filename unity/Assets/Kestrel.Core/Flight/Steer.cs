// Portrait thumb steering. The curve and the limits are data/steer.json.
// Physics forces are unchanged: this only maps thumb pixels to an axis in [-1, 1].
using System;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Flight
{
    public sealed class SteerTune
    {
        public readonly double DeadPx, FullPx, FullPxCompact, CompactMaxWidth, Expo;
        public readonly double SensitivityDefault, SensitivityMin, SensitivityMax;

        public static SteerTune Load(string repoRoot)
        {
            string path = Path.Combine(repoRoot, "data", "steer.json");
            return FromJson(MiniJson.ParseObject(File.ReadAllText(path)));
        }

        public static SteerTune FromJson(JsonObject o)
        {
            var sens = o.Obj("sensitivity");
            return new SteerTune(
                o.Num("deadPx"), o.Num("fullPx"), o.Num("fullPxCompact"), o.Num("compactMaxWidth"), o.Num("expo"),
                sens.Num("default"), sens.Num("min"), sens.Num("max"));
        }

        SteerTune(double deadPx, double fullPx, double fullPxCompact, double compactMaxWidth, double expo,
            double sensitivityDefault, double sensitivityMin, double sensitivityMax)
        {
            DeadPx = deadPx; FullPx = fullPx; FullPxCompact = fullPxCompact; CompactMaxWidth = compactMaxWidth; Expo = expo;
            SensitivityDefault = sensitivityDefault; SensitivityMin = sensitivityMin; SensitivityMax = sensitivityMax;
        }

        /// <summary>Small portrait phones (width &gt; 0 and &lt;= compactMaxWidth) use the shorter travel.</summary>
        public double FullTravel(double viewportWidth)
        {
            if (!double.IsNaN(viewportWidth) && !double.IsInfinity(viewportWidth) && viewportWidth > 0 && viewportWidth <= CompactMaxWidth)
                return FullPxCompact;
            return FullPx;
        }

        public double ClampSensitivity(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return SensitivityDefault;
            if (value < SensitivityMin) return SensitivityMin;
            if (value > SensitivityMax) return SensitivityMax;
            return value;
        }

        /// <summary>sign(dx) * clamp(|dx| / (fullTravel / sensitivity), 0, 1) ^ expo, in [-1, 1].</summary>
        public double Curve(double dxPx, double sensitivity, double viewportWidth)
        {
            double span = Math.Max(1, FullTravel(viewportWidth) / ClampSensitivity(sensitivity));
            double t = dxPx / span;
            if (t > 1) t = 1;
            else if (t < -1) t = -1;
            double mag = Math.Pow(Math.Abs(t), Expo);
            double signed = (t < 0 ? -1 : t > 0 ? 1 : 0) * mag;
            if (signed > 1) return 1;
            if (signed < -1) return -1;
            return signed;
        }

        /// <summary>Deadzone holds until the drag arms. After that, a small drag still steers.</summary>
        public double Peek(double dxPx, bool armed, double sensitivity, double viewportWidth)
        {
            if (double.IsNaN(dxPx) || double.IsInfinity(dxPx)) return 0;
            if (!armed && Math.Abs(dxPx) < DeadPx) return 0;
            return Curve(dxPx, sensitivity, viewportWidth);
        }
    }
}

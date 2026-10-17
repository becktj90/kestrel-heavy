// One-tap repair from a post-flight card or a designer warning.
// The number that changes is the one the failure named. Speed shortfalls go through DvMarginFix.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;

namespace Kestrel.Core.Designer
{
    public static class DesignerRepair
    {
        public static bool NeedsDeltaV(string kind)
            => kind == "prop-depletion" || kind == "dv-margin";

        public static Edit Apply(Catalog catalog, JsonObject design, string kind)
        {
            if (design == null || string.IsNullOrEmpty(kind)) return Edit.None;
            switch (kind)
            {
                case "overg":
                case "payload-g":
                    return LowerGLimit(catalog, design);
                case "tumble":
                    return HangarEditor.SetFins(design, true);
                case "structural-maxq":
                case "maxq-high":
                    return RaiseSafety(design);
                default:
                    return Edit.None;
            }
        }

        static Edit LowerGLimit(Catalog catalog, JsonObject design)
        {
            double cap = 8;
            if (catalog != null)
            {
                var payload = design.Obj("payload");
                string type = payload != null ? payload.Str("type") : null;
                if (type != null && catalog.PayloadById.TryGetValue(type, out var pl) && pl.maxG > 0 && pl.maxG < cap)
                    cap = pl.maxG;
            }
            double target = Math.Max(1.5, cap - 0.5);
            var g = design.Obj("guidance");
            if (g == null)
            {
                g = new JsonObject();
                JsonMut.Set(design, "guidance", g);
            }
            double current = g.ContainsKey("gLimit") ? g.Num("gLimit") : cap;
            if (current <= target + 1e-6) return Edit.None;
            JsonMut.Set(g, "gLimit", target);
            return new Edit("g limit", current.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                target.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
        }

        static Edit RaiseSafety(JsonObject design)
        {
            double step = DesignerLimits.Active.safetyStep;
            if (!(step > 0)) step = 0.05;
            double current = design.Num("safetyFactor", 1.4);
            return HangarEditor.SetSafetyFactor(design, current + step);
        }
    }
}

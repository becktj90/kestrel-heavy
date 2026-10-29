// Designer ranges, steps and thresholds. data/designer-limits.json is the source.
// Fallback() is the historical set (Validate MIN_TWR/GOOD_TWR/MIN_MARGIN/PACK and Stack.TUNE_LIMIT).
// Load() replaces Active from the file. A golden test asserts the file still holds those values.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;

namespace Kestrel.Core.Designer
{
    public sealed class ClusterPatternLimit
    {
        public string id;
        public bool fixedCount;
        public int count, countMin, countMax;
        public bool hasMinDiameter;
        public double minDiameterOverExit;
    }

    public sealed class DesignerLimits
    {
        public int stagesMin = 1, stagesMax = 5;
        public int engineCountMin = 1, engineCountMax = 40;
        public double diameterMin = 0.3, diameterMax = 15;
        public double safetyMin = 1, safetyMax = 3, safetyStep = 0.05;
        public double tuneLimitPct = 15;
        public double fairingLengthMin = 1, fairingLengthMax = 40;
        public double jettisonMinKm = 80, jettisonMaxKm = 150;
        public double payloadMassMin = 10, payloadMassMax = 100000;
        public double propMassMin = 1, propMassMax = 4000000;
        public int finCountMin = 3, finCountMax = 8;
        public double finSpanMin = 0.2, finSpanMax = 6, finSpanStep = 0.1;
        public double landingReserveMin = 0, landingReserveMax = 0.3;
        /// <summary>v3 guidance.pitchKickAltM. A campaign kick at 12000 m is inside this range.</summary>
        public double pitchKickAltMin = 0, pitchKickAltMax = 50000;
        public double minTwr = 1, goodTwr = 1.25, minMargin = 0.05, pack = 1.1;
        public double topHeavyRatio = 1.6, fairingNarrowRatio = 0.5;
        /// <summary>Fairing joint and flare (designer-limits fairingFlare). The advisory fires over warnRatio or warnAngleDeg; the shoulder is a cone of shoulderAngleDeg, at most shoulderMaxCylFraction of the cylinder.</summary>
        public double fairingFlareWarnRatio = 1.5, fairingFlareWarnAngleDeg = 30, fairingShoulderAngleDeg = 15, fairingShoulderMaxCylFraction = 0.5;
        public double weakUpperTwrVac = 0.5, weakUpperDvFraction = 0.2, heavyFairingKg = 500;
        public double stabilityAimMin = 1, stabilityAimMax = 2, overstableAbove = 4;
        public double maxQWarnKPa = 45;
        public double interstageLengthFactor = 0.6, interstageDragCd = 0.02;
        public readonly List<int> strapOnCounts = new List<int> { 0, 2, 4 };
        public readonly List<ClusterPatternLimit> patterns = new List<ClusterPatternLimit>();
        public readonly Dictionary<string, double> areal = new Dictionary<string, double> { ["composite"] = 14, ["al-li"] = 20, ["steel"] = 45 };
        public double arealDefault = 20;

        public static DesignerLimits Active = Fallback();

        public double Areal(string material) => material != null && areal.TryGetValue(material, out var k) ? k : arealDefault;

        public bool TryPattern(string id, out ClusterPatternLimit p)
        {
            p = null;
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < patterns.Count; i++) if (patterns[i].id == id) { p = patterns[i]; return true; }
            return false;
        }

        public static DesignerLimits Load(string repoRoot)
        {
            string path = Path.Combine(repoRoot, "data", "designer-limits.json");
            Active = Parse(MiniJson.ParseObject(File.ReadAllText(path)));
            return Active;
        }

        public static DesignerLimits Fallback()
        {
            var l = new DesignerLimits();
            SeedPatterns(l);
            return l;
        }

        static void SeedPatterns(DesignerLimits l)
        {
            l.patterns.Clear();
            void Fix(string id, int count, double minD) => l.patterns.Add(new ClusterPatternLimit { id = id, fixedCount = true, count = count, countMin = count, countMax = count, hasMinDiameter = true, minDiameterOverExit = minD });
            void Any(string id) => l.patterns.Add(new ClusterPatternLimit { id = id, countMin = 1, countMax = 40 });
            Fix("single", 1, 1);
            Fix("row2", 2, 2);
            Fix("row3", 3, 3);
            Fix("ring7", 7, 3);
            Fix("r1p8", 9, 3.61);
            Fix("r3p9", 12, 4.15);
            Fix("r3p10p20", 33, 7.39);
            Any("custom");
            Any("auto");
        }

        public static DesignerLimits Parse(JsonObject o)
        {
            var l = new DesignerLimits();
            var st = o.Obj("stages"); if (st != null) { l.stagesMin = (int)st.Num("min", 1); l.stagesMax = (int)st.Num("max", 5); }
            var ec = o.Obj("engineCount"); if (ec != null) { l.engineCountMin = (int)ec.Num("min", 1); l.engineCountMax = (int)ec.Num("max", 40); }
            var di = o.Obj("diameterM"); if (di != null) { l.diameterMin = di.Num("min", 0.3); l.diameterMax = di.Num("max", 15); }
            var sf = o.Obj("safetyFactor"); if (sf != null) { l.safetyMin = sf.Num("min", 1); l.safetyMax = sf.Num("max", 3); l.safetyStep = sf.Num("step", 0.05); }
            var tu = o.Obj("tunePct"); if (tu != null) l.tuneLimitPct = tu.Num("limit", 15);
            var fl = o.Obj("fairingLengthM"); if (fl != null) { l.fairingLengthMin = fl.Num("min", 1); l.fairingLengthMax = fl.Num("max", 40); }
            var je = o.Obj("jettisonAltitudeKm"); if (je != null) { l.jettisonMinKm = je.Num("min", 80); l.jettisonMaxKm = je.Num("max", 150); }
            var pm = o.Obj("payloadMassKg"); if (pm != null) { l.payloadMassMin = pm.Num("min", 10); l.payloadMassMax = pm.Num("max", 100000); }
            var pr = o.Obj("propMassKg"); if (pr != null) { l.propMassMin = pr.Num("min", 1); l.propMassMax = pr.Num("max", 4000000); }
            var fi = o.Obj("fins");
            if (fi != null)
            {
                l.finCountMin = (int)fi.Num("countMin", 3); l.finCountMax = (int)fi.Num("countMax", 8);
                l.finSpanMin = fi.Num("spanMinM", 0.2); l.finSpanMax = fi.Num("spanMaxM", 6); l.finSpanStep = fi.Num("spanStepM", 0.1);
            }
            var lr = o.Obj("landingReserveFraction"); if (lr != null) { l.landingReserveMin = lr.Num("min", 0); l.landingReserveMax = lr.Num("max", 0.3); }
            var tw = o.Obj("twr"); if (tw != null) { l.minTwr = tw.Num("min", 1); l.goodTwr = tw.Num("good", 1.25); }
            var mg = o.Obj("margin"); if (mg != null) l.minMargin = mg.Num("min", 0.05);
            if (o.ContainsKey("pack")) l.pack = o.Num("pack", 1.1);
            if (o.ContainsKey("topHeavyRatio")) l.topHeavyRatio = o.Num("topHeavyRatio", 1.6);
            if (o.ContainsKey("fairingNarrowRatio")) l.fairingNarrowRatio = o.Num("fairingNarrowRatio", 0.5);
            var ff = o.Obj("fairingFlare"); if (ff != null) { l.fairingFlareWarnRatio = ff.Num("warnRatio", 1.5); l.fairingFlareWarnAngleDeg = ff.Num("warnAngleDeg", 30); l.fairingShoulderAngleDeg = ff.Num("shoulderAngleDeg", 15); l.fairingShoulderMaxCylFraction = ff.Num("shoulderMaxCylFraction", 0.5); }
            if (o.ContainsKey("weakUpperTwrVac")) l.weakUpperTwrVac = o.Num("weakUpperTwrVac", 0.5);
            if (o.ContainsKey("weakUpperDvFraction")) l.weakUpperDvFraction = o.Num("weakUpperDvFraction", 0.2);
            if (o.ContainsKey("heavyFairingKg")) l.heavyFairingKg = o.Num("heavyFairingKg", 500);
            var sb = o.Obj("stabilityCal"); if (sb != null) { l.stabilityAimMin = sb.Num("aimMin", 1); l.stabilityAimMax = sb.Num("aimMax", 2); l.overstableAbove = sb.Num("overstableAbove", 4); }
            var mq = o.Obj("maxQKPa"); if (mq != null) l.maxQWarnKPa = mq.Num("warnAbove", 45);
            var inter = o.Obj("interstage"); if (inter != null) { l.interstageLengthFactor = inter.Num("lengthOverDiameter", 0.6); l.interstageDragCd = inter.Num("dragDeltaCd", 0.02); }
            var ar = o.Obj("arealKgPerM2");
            if (ar != null)
            {
                l.areal.Clear();
                foreach (var k in ar.Order) if (k != "default" && ar[k] is double d) l.areal[k] = d;
                l.arealDefault = ar.Num("default", 20);
            }
            var straps = o.Arr("strapOnCounts");
            if (straps != null) { l.strapOnCounts.Clear(); foreach (var x in straps) if (x is double d) l.strapOnCounts.Add((int)d); }
            var cl = o.Obj("cluster");
            var pats = cl?.Arr("patterns");
            if (pats != null)
            {
                l.patterns.Clear();
                foreach (JsonObject p in pats)
                {
                    var row = new ClusterPatternLimit { id = p.Str("id"), countMin = (int)p.Num("countMin", 1), countMax = (int)p.Num("countMax", 40) };
                    if (p.ContainsKey("count")) { row.fixedCount = true; row.count = (int)p.Num("count"); row.countMin = row.count; row.countMax = row.count; }
                    if (p.ContainsKey("minDiameterOverExit")) { row.hasMinDiameter = true; row.minDiameterOverExit = p.Num("minDiameterOverExit"); }
                    l.patterns.Add(row);
                }
            }
            else SeedPatterns(l);
            var kick = o.Obj("v3")?.Obj("guidance")?.Obj("pitchKickAltM");
            if (kick != null)
            {
                l.pitchKickAltMin = kick.Num("min", 0);
                l.pitchKickAltMax = kick.Num("max", 50000);
            }
            return l;
        }
    }
}

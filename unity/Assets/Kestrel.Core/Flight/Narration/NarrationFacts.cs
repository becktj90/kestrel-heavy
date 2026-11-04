using System;
using System.Collections.Generic;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight.Narration
{
    /// <summary>
    /// The facts a narration take may claim about the rocket being flown (docs/narration-facts.md).
    /// Values are int, bool or string. A fact that is not known is absent, and a required key that is
    /// absent is false, so the picker fails closed. Built once per flight from the Design, then given a
    /// lit stage per event (<see cref="WithLit"/>).
    /// </summary>
    public sealed class NarrationFacts
    {
        public const string Kerolox = "kerolox", Hydrolox = "hydrolox", Methalox = "methalox", Solid = "solid", Storable = "storable";

        readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public int Stages => values.TryGetValue("stages", out var v) && v is int n ? n : 0;
        public bool Fins => values.TryGetValue("fins", out var v) && v is bool b && b;
        public bool Finless => values.TryGetValue("finless", out var v) && v is bool b && b;
        public string Site => values.TryGetValue("site", out var v) ? v as string : null;
        public string Recovery => values.TryGetValue("recovery", out var v) ? v as string : null;

        public bool TryGet(string key, out object value) => values.TryGetValue(key ?? "", out value);

        public int EngineCount(int stage)
            => values.TryGetValue("stage" + stage + ".engines", out var v) && v is int n ? n : 0;

        /// <summary>Total engines in the first stage and the strap-ons, the number a "6 engines" line may state.</summary>
        public int LiftoffEngines
        {
            get
            {
                int n = EngineCount(1);
                if (values.TryGetValue("strapons", out var c) && c is int sc && sc > 0
                    && values.TryGetValue("strapons.engines", out var e) && e is int se) n += sc * se;
                return n;
            }
        }

        public static string PropellantId(string propellant)
        {
            switch (propellant)
            {
                case "kerolox": return Kerolox;
                case "hydrolox": return Hydrolox;
                case "methalox": return Methalox;
                case "solid-apcp": return Solid;
                case "mmh-nto": return Storable;
                default: return null;
            }
        }

        /// <summary>The picked landing: barge, rtls, or none. A booster that was not given a landing is never "coming home".</summary>
        public static string RecoveryOf(Design d)
        {
            string mode = d != null ? d.recoveryMode : null;
            return RecoverySim.Offered(mode) ? mode : "none";
        }

        /// <summary>Facts of a flown design on a pad. A null or empty site leaves <c>site</c> absent (site-gated takes then stay silent).</summary>
        public static NarrationFacts From(Design d, string siteId)
        {
            var f = new NarrationFacts();
            if (d == null || d.stages == null || d.stages.Count == 0) return f;
            var v = f.values;
            v["stages"] = d.stages.Count;
            for (int i = 0; i < d.stages.Count && i < 9; i++)
            {
                var s = d.stages[i];
                v["stage" + (i + 1) + ".engines"] = s != null ? Math.Max(1, s.engineCount) : 1;
                string p = s != null ? PropellantId(s.propellant) : null;
                if (p != null) v["stage" + (i + 1) + ".propellant"] = p;
            }
            int straps = d.StrapOnCount;
            v["strapons"] = straps;
            v["strapons.engines"] = straps > 0 ? Math.Max(1, d.strapOns.engineCount) : 0;
            string sp = straps > 0 ? PropellantId(d.strapOns.propellant) : null;
            if (sp != null) v["strapons.propellant"] = sp;
            // propellant.all: one id when every stage and the strap-ons burn the same thing, "mixed" when not,
            // absent when any of them is unmapped. Strap-ons with no propellant of their own count as the first stage's.
            var all = new List<string>();
            for (int i = 0; i < d.stages.Count; i++) all.Add(d.stages[i] != null ? PropellantId(d.stages[i].propellant) : null);
            if (straps > 0) all.Add(sp ?? all[0]);
            bool known = true, same = true;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null) known = false;
                else if (all[i] != all[0]) same = false;
            }
            if (known) v["propellant.all"] = same ? all[0] : "mixed";
            v["legs"] = d.legs;
            v["recovery"] = RecoveryOf(d);
            v["fairing"] = Kestrel.Core.Flight.MissionFlightSetup.FairingJettisonAltM(d) > 0;
            v["fins"] = d.fins;
            v["finless"] = !d.fins;
            if (!string.IsNullOrEmpty(siteId)) v["site"] = siteId;
            return f;
        }

        /// <summary>A copy that adds the lit stage (1-based core stage) keys: lit.stage, lit.engines, lit.propellant, lit.last.</summary>
        public NarrationFacts WithLit(int litStage)
        {
            var f = new NarrationFacts();
            foreach (var kv in values) f.values[kv.Key] = kv.Value;
            f.values.Remove("lit.stage"); f.values.Remove("lit.engines"); f.values.Remove("lit.propellant"); f.values.Remove("lit.last");
            int n = Stages;
            if (litStage < 1 || (n > 0 && litStage > n)) return f;
            f.values["lit.stage"] = litStage;
            if (values.TryGetValue("stage" + litStage + ".engines", out var e)) f.values["lit.engines"] = e;
            if (values.TryGetValue("stage" + litStage + ".propellant", out var p)) f.values["lit.propellant"] = p;
            f.values["lit.last"] = litStage == n;
            return f;
        }

        /// <summary>Test and diagnostic helper: a copy with one fact set.</summary>
        public NarrationFacts With(string key, object value)
        {
            var f = new NarrationFacts();
            foreach (var kv in values) f.values[kv.Key] = kv.Value;
            if (value == null) f.values.Remove(key); else f.values[key] = value;
            return f;
        }

        public IEnumerable<KeyValuePair<string, object>> All => values;
    }
}

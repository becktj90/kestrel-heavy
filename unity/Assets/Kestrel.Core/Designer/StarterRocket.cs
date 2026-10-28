// Phase 1 "Starter" designer (docs/plans/PHASED_DESIGNER_PLAN.md, PR 5). Four controls on top of the
// mission's campaign winner (data/golden/campaign-winners.json): engine fuel, stage count, fuel size
// (5 notches, -20%..+20%) and boosters 0/2/4.
//
// A thin adapter over the real designer. BuildDesign replays the controls onto a fresh copy of the base
// design with the full designer's own edit ops (HangarEditor.RemoveStage / AddStage / SetPropellant /
// SetEngine / SetEngineCount / SetPropMass / SetBoosterCount), so Launch flies exactly the JSON the hangar
// would. There is no table of approved combinations: a bad but buildable rocket launches, and may explode.
// Only a hard schema block from Validate.Engineering (no engines, cargo that does not fit) stops Launch.
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    /// <summary>Fuel for the first stage (and any middle stage). A hydrogen upper stage keeps hydrogen.</summary>
    public enum StarterEngine { Kerosene = 0, Methane = 1 }

    /// <summary>Which control the player just moved (for the hint line).</summary>
    public enum StarterKnob { None, Engine, Stages, FuelNotch, Boosters }

    /// <summary>One setting of the four controls. FuelNotch is an index 0..4 into <see cref="StarterRocket.FuelPercents"/>.</summary>
    public readonly struct StarterChoice : IEquatable<StarterChoice>
    {
        public readonly StarterEngine Engine;
        public readonly int Stages;
        public readonly int FuelNotch;
        public readonly int Boosters;

        public StarterChoice(StarterEngine engine, int stages, int fuelNotch, int boosters)
        {
            Engine = engine; Stages = stages; FuelNotch = fuelNotch; Boosters = boosters;
        }

        public int FuelPercent => StarterRocket.FuelPercentOf(FuelNotch);
        public StarterChoice WithEngine(StarterEngine v) => new StarterChoice(v, Stages, FuelNotch, Boosters);
        public StarterChoice WithStages(int v) => new StarterChoice(Engine, v, FuelNotch, Boosters);
        public StarterChoice WithFuelNotch(int v) => new StarterChoice(Engine, Stages, v, Boosters);
        public StarterChoice WithBoosters(int v) => new StarterChoice(Engine, Stages, FuelNotch, v);

        /// <summary>Stable id, e.g. "kerosene-2-f2-b0".</summary>
        public string Key => StarterRocket.EngineId(Engine) + "-" + Stages + "-f" + FuelNotch + "-b" + Boosters;

        public bool Equals(StarterChoice o) => Engine == o.Engine && Stages == o.Stages && FuelNotch == o.FuelNotch && Boosters == o.Boosters;
        public override bool Equals(object obj) => obj is StarterChoice o && Equals(o);
        public override int GetHashCode() => ((((int)Engine * 8 + Stages) * 8 + FuelNotch) * 8) + Boosters;
        public override string ToString() => Key;
    }

    /// <summary>What the existing readout says about the built rocket. Plain information, never a gate,
    /// except <see cref="Blocked"/> (a hard schema block from Validate.Engineering).</summary>
    public sealed class StarterCheck
    {
        public bool Blocked;
        /// <summary>The one plain reason when <see cref="Blocked"/> (the first block's message).</summary>
        public string BlockReason;
        /// <summary>Validate's code for the block (e.g. "twr-low", "payload-doesnt-fit").</summary>
        public string BlockCode;
        /// <summary>False when the readout could not compute (no numbers to show).</summary>
        public bool HasNumbers;
        public double LiftoffTwr, DeltaV, Required;
        /// <summary>DeltaV / Required - 1 (0.08 = 8% to spare; negative = short).</summary>
        public double Margin;
        /// <summary>The readout's fairing fit (null when the payload has no envelope).</summary>
        public bool? FairingFits;
    }

    /// <summary>Each mission's base rocket: its campaign winner. The host sets <see cref="Active"/> at boot.</summary>
    public static class StarterBases
    {
        public static IReadOnlyDictionary<string, JsonObject> Active;

        /// <summary>{"missions":[{"id":"m1","designs":[{...}]}]}: the first design per mission.</summary>
        public static Dictionary<string, JsonObject> Parse(string json)
        {
            var map = new Dictionary<string, JsonObject>();
            if (string.IsNullOrWhiteSpace(json)) return map;
            var file = MiniJson.ParseObject(json);
            var rows = file.Arr("missions");
            if (rows == null) return map;
            foreach (var o in rows)
                if (o is JsonObject row && row.Str("id") is string id && row.Arr("designs") is List<object> ds && ds.Count > 0 && ds[0] is JsonObject d)
                    map[id] = d;
            return map;
        }

        /// <summary>A fresh copy of the mission's base design, or null.</summary>
        public static JsonObject For(string missionId)
        {
            if (Active == null || missionId == null || !Active.TryGetValue(missionId, out var d) || d == null) return null;
            return JsonMut.CloneObject(d);
        }
    }

    /// <summary>
    /// The Starter designer for one mission. Setting a control clamps it to the offered values and raises
    /// <see cref="Changed"/>. Deterministic (no randomness, no clocks). Not thread-safe.
    /// </summary>
    public sealed class StarterRocket
    {
        public const int FuelNotches = 5;
        public const int DefaultFuelNotch = 2;
        public const int SaveVersion = 2;
        /// <summary>Fuel size per notch, in percent of the base rocket's propellant.</summary>
        public static readonly IReadOnlyList<int> FuelPercents = new[] { -20, -10, 0, 10, 20 };
        public static readonly IReadOnlyList<StarterEngine> EngineOrder = new[] { StarterEngine.Kerosene, StarterEngine.Methane };
        public static readonly IReadOnlyList<int> StageValues = new[] { 1, 2, 3 };
        public static readonly IReadOnlyList<int> BoosterValues = new[] { 0, 2, 4 };

        readonly Catalog catalog;
        readonly JsonObject baseDesign;
        StarterChoice choice;
        JsonObject built; StarterChoice builtFor; bool hasBuilt;
        StarterCheck check; StarterChoice checkFor; bool hasCheck;

        /// <param name="baseDesign">The mission's base rocket (campaign winner, or the design on the pad). Copied.</param>
        public StarterRocket(Catalog catalog, CampaignMission mission, JsonObject baseDesign)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (baseDesign == null) throw new ArgumentNullException(nameof(baseDesign));
            Mission = mission;
            MissionId = mission?.Id;
            this.baseDesign = JsonMut.CloneObject(baseDesign);
            Default = ChoiceOf(this.baseDesign, mission);
            choice = Default;
        }

        public string MissionId { get; }
        public CampaignMission Mission { get; }
        public Catalog Catalog => catalog;
        /// <summary>A fresh copy of the base rocket (Reset goes back to this).</summary>
        public JsonObject BaseDesign => JsonMut.CloneObject(baseDesign);

        /// <summary>Raised after the setting changes (control, Reset, ApplySave). Not raised when nothing changed.</summary>
        public event Action<StarterRocket> Changed;

        public StarterChoice Choice => choice;
        /// <summary>The base rocket's own setting: its engine fuel, stage count and boosters at the middle fuel notch.</summary>
        public StarterChoice Default { get; }

        // ---- the four controls (setters clamp to the offered values) ----
        public StarterEngine Engine { get => choice.Engine; set => Set(choice.WithEngine(value)); }
        public int Stages { get => choice.Stages; set => Set(choice.WithStages(value)); }
        /// <summary>0..4; see <see cref="FuelPercent"/>.</summary>
        public int FuelNotch { get => choice.FuelNotch; set => Set(choice.WithFuelNotch(value)); }
        /// <summary>0, 2 or 4.</summary>
        public int Boosters { get => choice.Boosters; set => Set(choice.WithBoosters(value)); }
        public int FuelPercent => choice.FuelPercent;

        // ---- what each control offers on this mission ----
        public bool IsEngineAllowed(StarterEngine v) => v == StarterEngine.Kerosene || v == StarterEngine.Methane;
        public bool IsStagesAllowed(int v) => v >= 1 && v <= Math.Min(3, DesignerLimits.Active.stagesMax);
        public bool IsFuelNotchAllowed(int v) => v >= 0 && v < FuelNotches;
        /// <summary>Strap-on missions offer 0/2/4. KH-10/KH-19 fly their landing pair. Others keep the base count.</summary>
        public bool IsBoostersAllowed(int v)
        {
            if (Mission != null && Mission.StrapOnsAllowed && !Mission.LandBoosters) return v == 0 || v == 2 || v == 4;
            return v == Default.Boosters;
        }

        /// <summary>Show the boosters control only where the mission has strap-ons.</summary>
        public bool BoostersVisible => Mission != null && (Mission.StrapOnsAllowed || Mission.LandBoosters);
        /// <summary>KH-10 and KH-19: shown, fixed to the landing pair.</summary>
        public bool BoostersLocked => BoostersVisible && !(Mission.StrapOnsAllowed && !Mission.LandBoosters);
        /// <summary>Informational: the upper stage burns hydrogen and is not a control.</summary>
        public bool HydrogenUpper
        {
            get
            {
                var st = Design().Arr("stages");
                return st != null && st.Count > 1 && st[st.Count - 1] is JsonObject top && top.Str("propellant") == "hydrolox";
            }
        }

        public static int FuelPercentOf(int notch) => notch >= 0 && notch < FuelNotches ? FuelPercents[notch] : 0;
        public static string EngineId(StarterEngine e) => e == StarterEngine.Methane ? "methane" : "kerosene";
        public static string PropellantOf(StarterEngine e) => e == StarterEngine.Methane ? "methalox" : "kerolox";
        public static bool TryEngine(string id, out StarterEngine e)
        {
            e = StarterEngine.Kerosene;
            if (id == "kerosene") return true;
            if (id == "methane") { e = StarterEngine.Methane; return true; }
            return false;
        }

        // ---- the real rocket ----

        /// <summary>The vehicle design for the current setting (a fresh copy; the JSON the hangar, PadHandoff and Launch use).</summary>
        public JsonObject BuildDesign() => JsonMut.CloneObject(Design());

        /// <summary>The design for any setting (a fresh copy). Does not change state.</summary>
        public JsonObject BuildDesign(StarterChoice c) => Build(catalog, baseDesign, Default, c, Mission);

        /// <summary>Validate.Engineering and the existing readout on the built rocket. Cached per setting.</summary>
        public StarterCheck Check()
        {
            if (hasCheck && checkFor.Equals(choice)) return check;
            check = CheckDesign(catalog, Design(), Mission);
            checkFor = choice; hasCheck = true;
            return check;
        }

        /// <summary>The same check on the base rocket (the mission's flown winner), for comparisons.</summary>
        public StarterCheck BaseCheck() => baseCheck ??= CheckDesign(catalog, Build(catalog, baseDesign, Default, Default, Mission), Mission);
        StarterCheck baseCheck;

        // ---- the launch predictor (Honest Ready) ----

        readonly Dictionary<StarterChoice, StarterVerdict> verdicts = new Dictionary<StarterChoice, StarterVerdict>();
        StarterPredictionJob job;

        /// <summary>The predictor's verdict for the current setting: one practice flight, cached per setting. Blocking
        /// (one full ascent on a miss); the screen uses <see cref="TryVerdict"/> and <see cref="StepPrediction"/>.</summary>
        public StarterVerdict Verdict() => Verdict(choice);

        /// <summary>The verdict for any setting (cached). Null for a hard-blocked setting, which never flies.</summary>
        public StarterVerdict Verdict(StarterChoice c)
        {
            if (verdicts.TryGetValue(c, out var v)) return v;
            var j = job != null && job.Choice.Equals(c) ? job : NewJob(c);
            if (j == null) return null;
            j.Step(double.PositiveInfinity);
            if (ReferenceEquals(j, job)) job = null;
            return verdicts[c] = j.Verdict;
        }

        /// <summary>The cached verdict for the current setting, without flying. False while it is still being checked.</summary>
        public bool TryVerdict(out StarterVerdict v) => verdicts.TryGetValue(choice, out v);

        /// <summary>True when the current setting is cached (or never flies because it is hard-blocked).</summary>
        public bool VerdictReady => verdicts.ContainsKey(choice) || Check().Blocked;

        /// <summary>
        /// Advance the predictor by at most <paramref name="budgetMs"/> of wall time: the current setting first, then
        /// each one-tap neighbour (one control moved one step), so the next tap usually reads from the cache.
        /// True when this call finished the current setting's verdict (the screen refreshes then).
        /// </summary>
        public bool StepPrediction(double budgetMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool current = false;
            // A tap moved off the setting being flown: fly the new one first.
            if (job != null && !job.Choice.Equals(choice) && !verdicts.ContainsKey(choice)) job = null;
            while (true)
            {
                if (job == null)
                {
                    if (!NextToPredict(out var next)) return current;
                    job = NewJob(next);
                    if (job == null) { verdicts[next] = null; continue; }
                }
                double left = budgetMs - sw.Elapsed.TotalMilliseconds;
                if (!job.Step(Math.Max(0, left))) return current;
                verdicts[job.Choice] = job.Verdict;
                if (job.Choice.Equals(choice)) current = true;
                job = null;
                if (sw.Elapsed.TotalMilliseconds >= budgetMs) return current;
            }
        }

        /// <summary>The settings one tap away from the current one, in the order the predictor warms them.</summary>
        public IEnumerable<StarterChoice> Neighbours()
        {
            var c = choice;
            foreach (var e in EngineOrder) if (e != c.Engine) yield return c.WithEngine(e);
            yield return c.WithFuelNotch(c.FuelNotch + 1);
            yield return c.WithFuelNotch(c.FuelNotch - 1);
            yield return c.WithStages(c.Stages + 1);
            yield return c.WithStages(c.Stages - 1);
            yield return c.WithBoosters(c.Boosters + 2);
            yield return c.WithBoosters(c.Boosters - 2);
        }

        bool NextToPredict(out StarterChoice next)
        {
            next = choice;
            if (!verdicts.ContainsKey(choice)) return true;
            foreach (var n in Neighbours())
            {
                if (!Clamp(n).Equals(n) || verdicts.ContainsKey(n)) continue;
                next = n;
                return true;
            }
            return false;
        }

        StarterPredictionJob NewJob(StarterChoice c)
        {
            var d = c.Equals(choice) ? Design() : Build(catalog, baseDesign, Default, c, Mission);
            var chk = c.Equals(choice) ? Check() : CheckDesign(catalog, d, Mission);
            if (chk.Blocked) return null;
            return new StarterPredictionJob(catalog, JsonMut.CloneObject(d), Mission, chk.FairingFits, c);
        }

        /// <summary>Can the player still move this control in the direction a fix names ("fuel-", "stages+", ...)?</summary>
        public bool CanFix(string fix)
        {
            var c = choice;
            switch (fix)
            {
                case "fuel-": return IsFuelNotchAllowed(c.FuelNotch - 1);
                case "fuel+": return IsFuelNotchAllowed(c.FuelNotch + 1);
                case "stages-": return IsStagesAllowed(c.Stages - 1);
                case "stages+": return IsStagesAllowed(c.Stages + 1);
                case "boosters-": return IsBoostersAllowed(c.Boosters - 2);
                case "boosters+": return IsBoostersAllowed(c.Boosters + 2);
                case "engine": return true;
                default: return false;
            }
        }

        /// <summary>True unless Validate reports a hard schema block. A rocket that will not reach orbit still launches.</summary>
        public bool CanLaunch => !Check().Blocked;

        /// <summary>Back to the base rocket (Starter has Reset instead of Undo/Redo).</summary>
        public void Reset() => Apply(Default);

        JsonObject Design()
        {
            if (!hasBuilt || !builtFor.Equals(choice))
            {
                built = Build(catalog, baseDesign, Default, choice, Mission);
                builtFor = choice; hasBuilt = true;
            }
            return built;
        }

        /// <summary>
        /// Replay <paramref name="want"/> onto a copy of <paramref name="baseDesign"/> with the full designer's ops:
        /// stages first (remove the middle, then the top; add a middle stage like the Hangar's "add stage"), then
        /// engine fuel on every non-hydrogen stage (nearest engine by thrust, count kept to the same total push),
        /// then each stage's propellant scaled by the fuel notch, then the booster count.
        /// </summary>
        public static JsonObject Build(Catalog c, JsonObject baseDesign, StarterChoice baseChoice, StarterChoice want, CampaignMission mission)
        {
            var d = JsonMut.CloneObject(baseDesign);
            var stages = d.Arr("stages");
            if (stages == null || stages.Count == 0) return d;
            double[] baseProp = null;

            // 1) stages
            int n = Math.Max(1, Math.Min(3, want.Stages));
            while (d.Arr("stages").Count > n)
            {
                int count = d.Arr("stages").Count;
                if (!HangarEditor.RemoveStage(d, count > 2 ? count - 2 : count - 1).changed) break;
            }
            while (d.Arr("stages").Count < n)
                if (!HangarEditor.AddStage(c, d, 1).changed) break;
            stages = d.Arr("stages");

            // 2) engine fuel (not the hydrogen upper)
            string prop = PropellantOf(want.Engine);
            for (int i = 0; i < stages.Count; i++)
            {
                var s = (JsonObject)stages[i];
                string cur = s.Str("propellant");
                if (cur != "kerolox" && cur != "methalox") continue;
                if (cur != prop) SwapFuel(c, d, PartRef.Stage(i), prop);
            }
            var so = d.Obj("strapOns");
            if (so != null && so.Str("kind") == "liquid" && (so.Str("propellant") == "kerolox" || so.Str("propellant") == "methalox") && so.Str("propellant") != prop)
                SwapFuel(c, d, PartRef.Boosters, prop);

            // Engines that no longer fit under their body (a stage added on a narrow rocket, or a new fuel's
            // engine) move to the nearest engine of that fuel that fits, as few as needed for the same push.
            for (int i = 0; i < stages.Count; i++) EnsureFit(c, d, PartRef.Stage(i));
            if (d.Obj("strapOns") is JsonObject sob && sob.Num("count") > 0) EnsureFit(c, d, PartRef.Boosters);

            // 3) fuel notch: every core stage scales from what it carries after steps 1-2
            double k = 1 + FuelPercentOf(Math.Max(0, Math.Min(FuelNotches - 1, want.FuelNotch))) / 100.0;
            if (k != 1)
            {
                baseProp = new double[stages.Count];
                for (int i = 0; i < stages.Count; i++) baseProp[i] = ((JsonObject)stages[i]).Num("propMass");
                for (int i = 0; i < stages.Count; i++) HangarEditor.SetPropMass(d, PartRef.Stage(i), baseProp[i] * k);
            }

            // 4) boosters (mission rules: only strap-on missions change the count)
            int b = want.Boosters;
            bool free = mission == null || (mission.StrapOnsAllowed && !mission.LandBoosters);
            if (!free) b = baseChoice.Boosters;
            int have = (int)(d.Obj("strapOns")?.Num("count") ?? 0);
            if (b != have && HangarEditor.SetBoosterCount(c, d, b).changed && b > 0) EnsureFit(c, d, PartRef.Boosters);

            // The mission stamp Home and the board use (payload type and mass, orbit, budget, missionId).
            if (mission != null) HomeAutoBuild.ApplyMission(c, d, mission);
            if (!want.Equals(baseChoice)) JsonMut.Set(d, "id", (baseDesign.Str("id") ?? "starter") + "~" + want.Key);
            return d;
        }

        /// <summary>SetPropellant, then the engine of that fuel closest in thrust (same sea-level/vacuum kind),
        /// with the count that keeps about the same total push. All three are HangarEditor ops.</summary>
        static void SwapFuel(Catalog c, JsonObject d, PartRef p, string prop)
        {
            var s = HangarParts.Body(d, p);
            if (s == null) return;
            c.EngineById.TryGetValue(s.Str("engine") ?? "", out var cur);
            int count = Math.Max(1, (int)s.Num("engineCount"));
            HangarEditor.SetPropellant(c, d, p, prop);
            if (cur == null) return;
            bool vac = cur.thrustSL == 0;
            double want = Math.Max(1, cur.thrustVac);
            Engine best = null; double bestGap = double.MaxValue;
            foreach (var e in c.Engines)
            {
                if (e.propellant != prop || (e.thrustSL == 0) != vac || !(e.thrustVac > 0)) continue;
                double gap = Math.Abs(Math.Log(e.thrustVac / want));
                if (gap < bestGap) { bestGap = gap; best = e; }
            }
            if (best == null) return;
            HangarEditor.SetEngine(c, d, p, best.id);
            int n = Math.Max(1, (int)Math.Round(count * want / best.thrustVac));
            HangarEditor.SetEngineCount(d, p, n);
        }

        /// <summary>Validate's engine-fit rule (count × engine diameter² ≤ pack × body diameter²).</summary>
        public static bool Fits(Catalog c, JsonObject part)
        {
            if (part == null || part.Str("engine") == null || !c.EngineById.TryGetValue(part.Str("engine"), out var e)) return true;
            double ed = EnginePoint.Of(e, part.Str("nozzle")).diameter, bd = part.Num("diameter");
            return !(part.Num("engineCount") * ed * ed > DesignerLimits.Active.pack * bd * bd);
        }

        /// <summary>When a part's engines do not fit, the same-fuel engine nearest in thrust that does fit, with
        /// the count closest to the old total push (at least one). HangarEditor.SetEngine / SetEngineCount.</summary>
        static void EnsureFit(Catalog c, JsonObject d, PartRef p)
        {
            var s = HangarParts.Body(d, p);
            if (s == null || Fits(c, s) || !c.EngineById.TryGetValue(s.Str("engine") ?? "", out var cur)) return;
            double total = Math.Max(1, cur.thrustVac) * Math.Max(1, s.Num("engineCount"));
            bool vac = cur.thrustSL == 0;
            double bd = s.Num("diameter"), pack = DesignerLimits.Active.pack;
            Engine best = null; int bestN = 0;
            // Same fuel and kind first; then any engine of that fuel (a small sea-level engine on a narrow upper stage).
            for (int pass = 0; pass < 2 && best == null; pass++)
            {
                double bestGap = double.MaxValue;
                foreach (var e in c.Engines)
                {
                    if (e.propellant != cur.propellant || !(e.thrustVac > 0)) continue;
                    if (pass == 0 && (e.thrustSL == 0) != vac) continue;
                    double ed = EnginePoint.Of(e, "default").diameter;
                    int max = ed > 0 ? (int)Math.Floor(pack * bd * bd / (ed * ed) + 1e-9) : 99;
                    if (max < 1) continue;
                    int n = Math.Max(1, Math.Min(max, (int)Math.Round(total / e.thrustVac)));
                    double gap = Math.Abs(Math.Log(n * e.thrustVac / total));
                    if (gap < bestGap) { bestGap = gap; best = e; bestN = n; }
                }
            }
            if (best == null) return;
            HangarEditor.SetEngine(c, d, p, best.id);
            HangarEditor.SetEngineCount(d, p, bestN);
        }

        /// <summary>Validate.Engineering for the hard blocks, then Readout.Quick for the numbers. Hops use the
        /// mission's own delta-v need (CampaignMission.RequiredDeltaV), as the flight HUD does.</summary>
        public static StarterCheck CheckDesign(Catalog c, JsonObject design, CampaignMission mission)
        {
            var r = new StarterCheck();
            EngineeringReport report;
            try { report = Validate.Engineering(c, design); }
            catch (Exception e) { return new StarterCheck { Blocked = true, BlockReason = "This rocket can't be built: " + e.Message }; }
            if (report.blocks.Count > 0)
            {
                r.Blocked = true;
                var b = report.blocks[0];
                r.BlockCode = b.code;
                r.BlockReason = PlainReason(b);
                return r;
            }
            QuickReadout q = report.readout;
            if (q == null)
            {
                try { q = Readout.Quick(c, design); } catch (Exception) { q = null; }
            }
            if (q == null) return r;
            r.HasNumbers = true;
            r.LiftoffTwr = q.liftoffTWR;
            r.DeltaV = q.totalDeltaV;
            r.Required = q.required;
            r.Margin = q.margin;
            r.FairingFits = q.fairingFits;
            if (mission != null && mission.TargetKind == "apogee")
            {
                double need = mission.RequiredDeltaV();
                if (need > 0) { r.Required = need; r.Margin = q.totalDeltaV / need - 1; }
            }
            if (double.IsNaN(r.Margin) || double.IsInfinity(r.Margin) || double.IsNaN(r.LiftoffTwr)) r.HasNumbers = false;
            return r;
        }

        /// <summary>The one plain reason for a block, short enough for the readiness line.</summary>
        public static string PlainReason(Issue b)
        {
            switch (b?.code)
            {
                case "twr-low": return "Too heavy to leave the pad. Try less fuel.";
                case "payload-doesnt-fit": return "The cargo doesn't fit under the nose cone.";
                case "engines-dont-fit": return "The engines don't fit under the rocket.";
            }
            return string.IsNullOrEmpty(b?.msg) ? "This rocket can't be built." : b.msg;
        }

        // ---- save ----
        /// <summary>NativeStore key for this mission's last Starter setting, e.g. "kh.save.v1.starter.m3".</summary>
        public static string SaveKey(string missionId) => NativeStore.Prefix + "starter." + missionId;

        /// <summary>{"v":2,"mission":"m3","engine":"methane","stages":2,"fuelNotch":2,"boosters":0}</summary>
        public JsonObject ToSave()
        {
            var o = new JsonObject();
            o.Add("v", (double)SaveVersion);
            o.Add("mission", MissionId ?? "");
            o.Add("engine", EngineId(choice.Engine));
            o.Add("stages", (double)choice.Stages);
            o.Add("fuelNotch", (double)choice.FuelNotch);
            o.Add("boosters", (double)choice.Boosters);
            return o;
        }

        /// <summary>Restore a saved setting (clamped). False and no change when the record is for another mission or unreadable.
        /// Version 1 records (the dropped placeholder table) are read the same way.</summary>
        public bool ApplySave(JsonObject saved)
        {
            if (saved == null || saved.Str("mission") != (MissionId ?? "")) return false;
            int v = (int)saved.Num("v");
            if (v != SaveVersion && v != 1) return false;
            if (!TryEngine(saved.Str("engine"), out var e)) return false;
            Apply(Clamp(new StarterChoice(e, (int)saved.Num("stages"), (int)saved.Num("fuelNotch"), (int)saved.Num("boosters"))));
            return true;
        }

        /// <summary>The setting a stored design sits on: first-stage fuel, stage count (1..3), boosters.</summary>
        public static StarterChoice ChoiceOf(JsonObject design, CampaignMission m)
        {
            var stages = design?.Arr("stages");
            int count = stages != null && stages.Count > 0 ? Math.Min(3, stages.Count) : 1;
            var first = stages != null && stages.Count > 0 ? stages[0] as JsonObject : null;
            var engine = first?.Str("propellant") == "methalox" ? StarterEngine.Methane : StarterEngine.Kerosene;
            int boosters = (int)(design?.Obj("strapOns")?.Num("count") ?? 0);
            if (boosters != 0 && boosters != 2 && boosters != 4) boosters = 0;
            return new StarterChoice(engine, count, DefaultFuelNotch, boosters);
        }

        // ---- internals ----
        StarterChoice Clamp(StarterChoice want)
        {
            var e = IsEngineAllowed(want.Engine) ? want.Engine : choice.Engine;
            int s = IsStagesAllowed(want.Stages) ? want.Stages : choice.Stages;
            int f = IsFuelNotchAllowed(want.FuelNotch) ? want.FuelNotch : choice.FuelNotch;
            int b = IsBoostersAllowed(want.Boosters) ? want.Boosters : (IsBoostersAllowed(choice.Boosters) ? choice.Boosters : Default.Boosters);
            return new StarterChoice(e, s, f, b);
        }

        void Set(StarterChoice want) => Apply(Clamp(want));

        void Apply(StarterChoice next)
        {
            if (next.Equals(choice)) return;
            choice = next;
            Changed?.Invoke(this);
        }
    }
}

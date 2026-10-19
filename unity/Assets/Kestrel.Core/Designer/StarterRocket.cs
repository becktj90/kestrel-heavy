// Phase 1 "Starter" designer (docs/plans/PHASED_DESIGNER_PLAN.md, PR 4). Four knobs on top of the
// mission's known winner: engine type, stage count, fuel size (5 notches, -20%..+20%) and boosters 0/2/4.
// The knobs clamp to a combination that has flown; they never warn.
//
// API STUB for the Starter screen (PR 5). The shape is final; the data is a placeholder:
// StarterTable.Placeholder offers every combination the mission rules allow and BuildDesign returns
// the mission's winner unchanged. PR 4 swaps in data/starter-rockets.json from tools/starter-gen
// (combinations that passed 5/5 on MissionFlightSetup) and applies the knobs to the design.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Save;

namespace Kestrel.Core.Designer
{
    /// <summary>First-stage fuel. The hydrogen upper stage is automatic and not a knob.</summary>
    public enum StarterEngine { Kerosene = 0, Methane = 1 }

    /// <summary>Which knob the player just moved. Snapping keeps that knob when it can.</summary>
    public enum StarterKnob { None, Engine, Stages, FuelNotch, Boosters }

    /// <summary>One combination of the four knobs. FuelNotch is an index 0..4 into <see cref="StarterRocket.FuelPercents"/>.</summary>
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

        /// <summary>Stable id, e.g. "kerosene-2-f2-b0". Also the starter-rockets.json entry key.</summary>
        public string Key => StarterRocket.EngineId(Engine) + "-" + Stages + "-f" + FuelNotch + "-b" + Boosters;

        public bool Equals(StarterChoice o) => Engine == o.Engine && Stages == o.Stages && FuelNotch == o.FuelNotch && Boosters == o.Boosters;
        public override bool Equals(object obj) => obj is StarterChoice o && Equals(o);
        public override int GetHashCode() => ((((int)Engine * 8 + Stages) * 8 + FuelNotch) * 8) + Boosters;
        public override string ToString() => Key;
    }

    /// <summary>The result of snapping a requested combination.</summary>
    public sealed class StarterResolution
    {
        public StarterChoice Requested;
        public StarterChoice Choice;
        /// <summary>True when <see cref="Choice"/> differs from <see cref="Requested"/>.</summary>
        public bool Snapped;
        /// <summary>The vehicle design for <see cref="Choice"/> (a fresh copy; same JSON as the hangar and PadHandoff use).</summary>
        public JsonObject Design;
    }

    /// <summary>Passing combinations per mission, plus each mission's base (winner) design.</summary>
    public sealed class StarterTable
    {
        sealed class Row
        {
            public CampaignMission Mission;
            public JsonObject BaseDesign;
            public StarterChoice Default;
            public readonly List<StarterChoice> Passing = new List<StarterChoice>();
        }

        readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();

        /// <summary>The table the Starter screen uses. The host sets it at boot; PR 4 loads data/starter-rockets.json.</summary>
        public static StarterTable Active;

        /// <summary>True until PR 4: entries come from mission rules, not from flown results.</summary>
        public bool IsPlaceholder { get; private set; }

        public bool Has(string missionId) => missionId != null && rows.ContainsKey(missionId);
        public CampaignMission Mission(string missionId) => Get(missionId).Mission;
        public StarterChoice Default(string missionId) => Get(missionId).Default;
        public IReadOnlyList<StarterChoice> Passing(string missionId) => Get(missionId).Passing;
        public JsonObject BaseDesign(string missionId) => Get(missionId).BaseDesign;

        Row Get(string missionId)
        {
            if (missionId == null || !rows.TryGetValue(missionId, out var r)) throw new KeyNotFoundException("no starter for mission " + missionId);
            return r;
        }

        /// <summary>Placeholder from data/campaign.json and data/golden/campaign-winners.json in a checkout.</summary>
        public static StarterTable LoadPlaceholder(string repoRoot)
        {
            var campaign = Campaign.Load(repoRoot);
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "golden", "campaign-winners.json")));
            var winners = new Dictionary<string, JsonObject>();
            foreach (var o in file.Arr("missions"))
                if (o is JsonObject row && row.Arr("designs") is List<object> ds && ds.Count > 0 && ds[0] is JsonObject d)
                    winners[row.Str("id")] = d;
            return Placeholder(campaign, id => winners.TryGetValue(id, out var d) ? d : null);
        }

        /// <summary>
        /// Every combination the mission rules allow: stages 1 on KH-1/KH-2 and 2–3 on orbital missions;
        /// boosters 0/2/4 where strap-ons are allowed, the winner's pair where boosters must land, else 0.
        /// The default is the winner's combination at the middle fuel notch.
        /// </summary>
        public static StarterTable Placeholder(Campaign campaign, Func<string, JsonObject> baseDesign)
        {
            var t = new StarterTable { IsPlaceholder = true };
            foreach (var m in campaign.Missions)
            {
                var design = baseDesign?.Invoke(m.Id);
                var row = new Row { Mission = m, BaseDesign = design };
                var def = ChoiceOf(design, StarterRocket.DefaultFuelNotch, m);
                row.Default = def;
                var stages = m.TargetKind == "apogee" ? new[] { 1 } : new[] { 2, 3 };
                int[] boosters = m.StrapOnsAllowed ? new[] { 0, 2, 4 } : new[] { def.Boosters };
                foreach (StarterEngine e in StarterRocket.EngineOrder)
                    foreach (int s in stages)
                        for (int f = 0; f < StarterRocket.FuelNotches; f++)
                            foreach (int b in boosters)
                                row.Passing.Add(new StarterChoice(e, s, f, b));
                if (!row.Passing.Contains(def)) row.Passing.Add(def);
                t.rows[m.Id] = row;
            }
            return t;
        }

        /// <summary>The knob values a stored vehicle design sits on, relative to the mission's base design.</summary>
        internal static StarterChoice ChoiceOf(JsonObject design, int fuelNotch, CampaignMission m)
        {
            var stages = design?.Arr("stages");
            int count = stages != null && stages.Count > 0 ? Math.Min(3, stages.Count) : (m != null && m.TargetKind == "apogee" ? 1 : 2);
            var first = stages != null && stages.Count > 0 ? stages[0] as JsonObject : null;
            var engine = first?.Str("propellant") == "methalox" ? StarterEngine.Methane : StarterEngine.Kerosene;
            int boosters = (int)(design?.Obj("strapOns")?.Num("count") ?? 0);
            if (boosters != 0 && boosters != 2 && boosters != 4) boosters = 0;
            return new StarterChoice(engine, count, fuelNotch, boosters);
        }
    }

    /// <summary>
    /// The Starter designer for one mission. Setting a knob snaps to the nearest passing combination and
    /// raises <see cref="Changed"/>. Not thread-safe; deterministic (no randomness, no clocks).
    /// </summary>
    public sealed class StarterRocket
    {
        public const int FuelNotches = 5;
        public const int DefaultFuelNotch = 2;
        public const int SaveVersion = 1;
        /// <summary>Fuel size per notch, in percent of the winner's propellant.</summary>
        public static readonly IReadOnlyList<int> FuelPercents = new[] { -20, -10, 0, 10, 20 };
        public static readonly IReadOnlyList<StarterEngine> EngineOrder = new[] { StarterEngine.Kerosene, StarterEngine.Methane };
        public static readonly IReadOnlyList<int> StageValues = new[] { 1, 2, 3 };
        public static readonly IReadOnlyList<int> BoosterValues = new[] { 0, 2, 4 };

        readonly StarterTable table;
        StarterChoice choice;

        public StarterRocket(StarterTable table, string missionId)
        {
            this.table = table ?? throw new ArgumentNullException(nameof(table));
            MissionId = missionId;
            Mission = table.Mission(missionId);
            choice = table.Default(missionId);
        }

        /// <summary>Uses <see cref="StarterTable.Active"/>.</summary>
        public static StarterRocket ForMission(string missionId)
            => new StarterRocket(StarterTable.Active ?? throw new InvalidOperationException("StarterTable.Active is not set"), missionId);

        public string MissionId { get; }
        public CampaignMission Mission { get; }
        public bool IsPlaceholder => table.IsPlaceholder;

        /// <summary>Raised after the combination changes (knob, Reset, ApplySave). Not raised when nothing changed.</summary>
        public event Action<StarterRocket> Changed;

        public StarterChoice Choice => choice;
        public StarterChoice Default => table.Default(MissionId);
        /// <summary>True when the last knob move or ApplySave landed somewhere other than what was asked.</summary>
        public bool LastSnapped { get; private set; }

        // ---- the four knobs (setters snap) ----
        public StarterEngine Engine { get => choice.Engine; set => Set(choice.WithEngine(value), StarterKnob.Engine); }
        public int Stages { get => choice.Stages; set => Set(choice.WithStages(value), StarterKnob.Stages); }
        /// <summary>0..4; see <see cref="FuelPercent"/>.</summary>
        public int FuelNotch { get => choice.FuelNotch; set => Set(choice.WithFuelNotch(value), StarterKnob.FuelNotch); }
        /// <summary>0, 2 or 4.</summary>
        public int Boosters { get => choice.Boosters; set => Set(choice.WithBoosters(value), StarterKnob.Boosters); }
        public int FuelPercent => choice.FuelPercent;

        // ---- options per knob: values offered for this mission, in display order ----
        public IReadOnlyList<StarterEngine> EngineOptions => Offered(EngineOrder, c => c.Engine);
        public IReadOnlyList<int> StageOptions => Offered(StageValues, c => c.Stages);
        public IReadOnlyList<int> FuelNotchOptions => Offered(new[] { 0, 1, 2, 3, 4 }, c => c.FuelNotch);
        public IReadOnlyList<int> BoosterOptions => Offered(BoosterValues, c => c.Boosters);

        /// <summary>A value is allowed when some passing combination uses it. Picking it may snap the other knobs.</summary>
        public bool IsEngineAllowed(StarterEngine v) => Any(c => c.Engine == v);
        public bool IsStagesAllowed(int v) => Any(c => c.Stages == v);
        public bool IsFuelNotchAllowed(int v) => Any(c => c.FuelNotch == v);
        public bool IsBoostersAllowed(int v) => Any(c => c.Boosters == v);

        /// <summary>Show the boosters control only where the mission allows strap-ons.</summary>
        public bool BoostersVisible => Mission.StrapOnsAllowed || Mission.LandBoosters;
        /// <summary>KH-10 and KH-19: shown, fixed to the landing pair.</summary>
        public bool BoostersLocked => BoosterOptions.Count <= 1;
        /// <summary>Informational: the upper stage burns hydrogen and is not a knob.</summary>
        public bool HydrogenUpper => choice.Stages > 1;

        // ---- readiness ----
        /// <summary>The one readiness line. Placeholder wording; UX owns the copy and PR 4 takes it from LaunchGate.</summary>
        public string ReadinessLine => "Ready. Reaches " + TargetText(Mission) + " with speed to spare.";
        /// <summary>Launch is always enabled on a passing combination (plan: one big Launch button).</summary>
        public bool CanLaunch => IsPassing(choice);

        public static int FuelPercentOf(int notch) => notch >= 0 && notch < FuelNotches ? FuelPercents[notch] : 0;
        public static string EngineId(StarterEngine e) => e == StarterEngine.Methane ? "methane" : "kerosene";
        public static bool TryEngine(string id, out StarterEngine e)
        {
            e = StarterEngine.Kerosene;
            if (id == "kerosene") return true;
            if (id == "methane") { e = StarterEngine.Methane; return true; }
            return false;
        }

        public bool IsPassing(StarterChoice c)
        {
            foreach (var p in table.Passing(MissionId)) if (p.Equals(c)) return true;
            return false;
        }

        /// <summary>Snap <paramref name="want"/> to the nearest passing combination, keeping <paramref name="keep"/> when possible. Does not change state.</summary>
        public StarterResolution Resolve(StarterChoice want, StarterKnob keep = StarterKnob.None)
        {
            var got = Snap(table.Passing(MissionId), want, keep);
            return new StarterResolution { Requested = want, Choice = got, Snapped = !got.Equals(want), Design = DesignFor(got) };
        }

        /// <summary>The vehicle design for the current combination.</summary>
        public JsonObject BuildDesign() => DesignFor(choice);

        /// <summary>Back to the mission default (Starter has Reset instead of Undo/Redo).</summary>
        public void Reset() { LastSnapped = false; Apply(Default); }

        // ---- save ----
        /// <summary>NativeStore key for this mission's last Starter combination, e.g. "kh.save.v1.starter.m3".</summary>
        public static string SaveKey(string missionId) => NativeStore.Prefix + "starter." + missionId;

        /// <summary>{"v":1,"mission":"m3","engine":"methane","stages":2,"fuelNotch":2,"boosters":0}</summary>
        public JsonObject ToSave()
        {
            var o = new JsonObject();
            o.Add("v", (double)SaveVersion);
            o.Add("mission", MissionId);
            o.Add("engine", EngineId(choice.Engine));
            o.Add("stages", (double)choice.Stages);
            o.Add("fuelNotch", (double)choice.FuelNotch);
            o.Add("boosters", (double)choice.Boosters);
            return o;
        }

        /// <summary>Restore a saved combination (snapped). False and no change when the record is for another mission or unreadable.</summary>
        public bool ApplySave(JsonObject saved)
        {
            if (saved == null || saved.Str("mission") != MissionId || (int)saved.Num("v") != SaveVersion) return false;
            if (!TryEngine(saved.Str("engine"), out var e)) return false;
            var want = new StarterChoice(e, (int)saved.Num("stages"), (int)saved.Num("fuelNotch"), (int)saved.Num("boosters"));
            var r = Resolve(want);
            LastSnapped = r.Snapped;
            Apply(r.Choice);
            return true;
        }

        /// <summary>
        /// The Starter combination closest to an existing vehicle design (e.g. a hangar slot or the draft), snapped.
        /// Fuel notch comes from the first stage's propellant relative to the mission's base design.
        /// </summary>
        public StarterResolution FromDesign(JsonObject design)
        {
            int notch = DefaultFuelNotch;
            var baseProp = (table.BaseDesign(MissionId)?.Arr("stages") is List<object> bs && bs.Count > 0 && bs[0] is JsonObject b0) ? b0.Num("propMass") : 0;
            var prop = (design?.Arr("stages") is List<object> ds && ds.Count > 0 && ds[0] is JsonObject d0) ? d0.Num("propMass") : 0;
            if (baseProp > 0 && prop > 0)
            {
                double pct = (prop / baseProp - 1) * 100, best = double.MaxValue;
                for (int i = 0; i < FuelNotches; i++)
                {
                    double gap = Math.Abs(FuelPercents[i] - pct);
                    if (gap < best) { best = gap; notch = i; }
                }
            }
            return Resolve(StarterTable.ChoiceOf(design, notch, Mission));
        }

        // ---- internals ----
        void Set(StarterChoice want, StarterKnob keep)
        {
            var got = Snap(table.Passing(MissionId), want, keep);
            LastSnapped = !got.Equals(want);
            Apply(got);
        }

        void Apply(StarterChoice next)
        {
            if (next.Equals(choice)) return;
            choice = next;
            Changed?.Invoke(this);
        }

        JsonObject DesignFor(StarterChoice c)
        {
            var b = table.BaseDesign(MissionId);
            if (b == null) return null;
            var d = JsonMut.CloneObject(b);
            // PR 4: apply engine type, stage count, fuel notch and boosters here, from the flown entry.
            // Mission stamp as HomeAutoBuild.ApplyMission leaves it; the payload is locked to the mission mass.
            var pl = d.Obj("payload");
            if (pl != null && Mission.MassKg > 0) JsonMut.Set(pl, "mass", Mission.MassKg);
            JsonMut.Set(d, "missionId", Mission.Id ?? "");
            return d;
        }

        bool Any(Func<StarterChoice, bool> f)
        {
            foreach (var p in table.Passing(MissionId)) if (f(p)) return true;
            return false;
        }

        IReadOnlyList<T> Offered<T>(IReadOnlyList<T> order, Func<StarterChoice, T> pick)
        {
            var list = new List<T>();
            foreach (var v in order)
                foreach (var p in table.Passing(MissionId))
                    if (EqualityComparer<T>.Default.Equals(pick(p), v)) { list.Add(v); break; }
            return list;
        }

        /// <summary>Nearest by weighted knob distance (engine 4, stage 3, booster pair 2, fuel notch 1); ties keep table order.</summary>
        internal static StarterChoice Snap(IReadOnlyList<StarterChoice> passing, StarterChoice want, StarterKnob keep)
        {
            if (passing == null || passing.Count == 0) return want;
            bool anyKeep = false;
            foreach (var p in passing) if (Keeps(p, want, keep)) { anyKeep = true; break; }
            StarterChoice best = passing[0];
            int bestCost = int.MaxValue;
            foreach (var p in passing)
            {
                if (anyKeep && !Keeps(p, want, keep)) continue;
                int cost = (p.Engine == want.Engine ? 0 : 4) + 3 * Math.Abs(p.Stages - want.Stages)
                    + Math.Abs(p.Boosters - want.Boosters) + Math.Abs(p.FuelNotch - want.FuelNotch);
                if (cost < bestCost) { bestCost = cost; best = p; }
            }
            return best;
        }

        static bool Keeps(StarterChoice p, StarterChoice want, StarterKnob keep)
        {
            switch (keep)
            {
                case StarterKnob.Engine: return p.Engine == want.Engine;
                case StarterKnob.Stages: return p.Stages == want.Stages;
                case StarterKnob.FuelNotch: return p.FuelNotch == want.FuelNotch;
                case StarterKnob.Boosters: return p.Boosters == want.Boosters;
                default: return true;
            }
        }

        static string TargetText(CampaignMission m)
        {
            if (m == null) return "the target";
            if (m.TargetKind == "c3") return "escape speed";
            double km = m.ApoKm;
            return km >= 10000 ? "the high orbit" : km.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " km";
        }
    }
}

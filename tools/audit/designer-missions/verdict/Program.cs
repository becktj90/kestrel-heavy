// Designer-verdict vs flight harness (read-only against the repo).
// For each design in data/designer-templates.json, data/presets.json, data/board-starters.json
// and every design embedded in data/golden/*.json:
//   designer side: Readout.Reference (fills the max-Q/predict cache like the debounced UI) then
//                  Validate.Engineering + Experience.Present("standard").
//   flight side:   FlightBatch.Fly for the 5 campaign seeds (the campaign "viable" judge), a raw
//                  mirror of Fly to get the SimResult (dvExpended, losses, events), and an emulation
//                  of the pad path (FlightDriver.Launch: dt 1/120, tMax 600, MissionAim, PostFlightSource gates).
// Usage: dotnet run -c Release -- [repoRoot] [outDir]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;

static class Program
{
    static Catalog cat;
    static double LiveTMax = Environment.GetEnvironmentVariable("LIVE_TMAX") is string lt ? double.Parse(lt, CultureInfo.InvariantCulture) : 600;
    static readonly Dictionary<string, AutoBuildChoice> HomeChoice = new Dictionary<string, AutoBuildChoice>();
    static Campaign camp;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    sealed class Case
    {
        public string src, label, missionId; public JsonObject design; public bool stamp;
    }

    sealed class Row
    {
        public string src, label, mission, kind;
        public string verdict, blocks, warns; public bool ok, ready;
        public double dv = double.NaN, req = double.NaN, margin = double.NaN, twr = double.NaN, mReq = double.NaN;
        public bool? dFairFits; public bool vPayloadFitBlock;
        public int passes = -1; public string outcomes = "", failures = "", gates = "";
        public double apoKm = double.NaN, periKm = double.NaN, idealDv = double.NaN, dvExp = double.NaN, lossG, lossD, lossS;
        public bool burnedOut; public double simTwr = double.NaN, simT;
        public string live = ""; public bool livePass; public double liveApo = double.NaN, livePeri = double.NaN;
        public string simple = "", mif = "", dvfix = "";
        public string err = ""; public bool nondet; public string nondetWhy = "";
        public List<string> flags = new List<string>();
    }

    static int Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : (System.Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT") ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../../..")));
        string outDir = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(outDir);
        cat = Catalog.LoadFromRepo(root);
        camp = Campaign.Load(root);
        if (args.Length > 2 && args[2] == "probe") { Probe(root); return 0; }
        if (args.Length > 2 && args[2] == "sens") { Sensitivity(root); return 0; }
        var cases = Collect(root);
        Console.Error.WriteLine($"cases: {cases.Count}");
        var rows = new List<Row>();
        int n = 0;
        foreach (var c in cases)
        {
            n++;
            if (n % 50 == 0) Console.Error.WriteLine($"  {n}/{cases.Count}");
            rows.Add(RunCase(c));
        }
        ParallelDeterminism(cases, rows);
        Write(outDir, rows);
        return 0;
    }

    // ---------------------------------------------------------------- collection
    static List<Case> Collect(string root)
    {
        var list = new List<Case>();
        var seen = new HashSet<string>();
        void Add(string src, string label, JsonObject d, string mission, bool stamp)
        {
            string key = (mission ?? "own") + "|" + JsonWriter.Stringify(d);
            if (!seen.Add(key)) return;
            list.Add(new Case { src = src, label = label, design = d, missionId = mission, stamp = stamp });
        }
        JsonObject Load(params string[] p) => MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, Path.Combine(p))));

        var tpl = Load("data", "designer-templates.json");
        foreach (JsonObject t in tpl.Arr("templates"))
        {
            var d = t.Obj("design");
            Add("templates", "tpl:" + t.Str("id"), d, null, false);
            foreach (var m in camp.Missions) Add("templates", "tpl:" + t.Str("id"), d, m.Id, true);
        }
        var temps = new List<Kestrel.Core.Hangar.StarterTemplate>();
        foreach (JsonObject t in tpl.Arr("templates"))
            temps.Add(new Kestrel.Core.Hangar.StarterTemplate { id = t.Str("id"), name = t.Str("name"), order = (int)t.Num("order"), version = (int)t.Num("version", 1), design = t.Obj("design") });
        foreach (var m in camp.Missions)
        {
            var ch = HomeAutoBuild.Choose(cat, temps, m);
            if (ch == null || ch.Design == null) continue;
            HomeChoice[m.Id] = ch;
            Add("home", "home:" + m.Id + ":" + ch.TemplateId + (ch.LaunchEnabled ? "" : "(launch-disabled)") + " reach=\"" + ch.ReachLine + "\"", ch.Design, m.Id, false);
        }
        var pre = Load("data", "presets.json");
        foreach (JsonObject p in pre.Arr("presets"))
        {
            Add("presets", "preset:" + p.Str("id"), p, null, false);
            foreach (var m in camp.Missions) Add("presets", "preset:" + p.Str("id"), p, m.Id, true);
        }
        var board = Load("data", "board-starters.json");
        foreach (JsonObject m in board.Arr("missions"))
        {
            int i = 0;
            foreach (JsonObject d in m.Arr("designs")) Add("board-starters", "board:" + m.Str("id") + "#" + (i++) + ":" + d.Str("id"), d, m.Str("id"), false);
        }
        foreach (var f in new[] { "campaign-winners", "campaign-viable" })
        {
            var g = Load("data", "golden", f + ".json");
            foreach (JsonObject m in g.Arr("missions"))
            {
                int i = 0;
                foreach (JsonObject d in m.Arr("designs")) Add("golden/" + f, f + ":" + m.Str("id") + "#" + (i++) + ":" + d.Str("id"), d, m.Str("id"), false);
            }
        }
        foreach (var path in Directory.GetFiles(Path.Combine(root, "data", "golden"), "*.json").OrderBy(x => x, StringComparer.Ordinal))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name == "campaign-winners" || name == "campaign-viable") continue;
            object doc;
            try { doc = MiniJson.Parse(File.ReadAllText(path)); } catch { continue; }
            var found = new List<(string, JsonObject)>();
            Walk(doc, "", found);
            foreach (var (p, d) in found) Add("golden/" + name, name + p, d, null, false);
        }
        return list;
    }

    static void Walk(object o, string path, List<(string, JsonObject)> outp)
    {
        if (o is JsonObject j)
        {
            var st = j.Arr("stages");
            if (st != null && st.Count > 0 && st[0] is JsonObject s0 && s0.ContainsKey("engine") && j.Obj("payload") != null) { outp.Add((path, j)); return; }
            foreach (var k in j.Order) Walk(j[k], path + "." + k, outp);
        }
        else if (o is List<object> a)
            for (int i = 0; i < a.Count; i++) Walk(a[i], path + "[" + i + "]", outp);
    }

    // ---------------------------------------------------------------- one case
    static CampaignMission OwnMission(Design d)
    {
        string oid = d.payloadOrbit;
        if (oid == null || !cat.Orbits.TryGetValue(oid, out var o)) return null;
        // A kh- orbit is a campaign target: fly that mission row with the design's own payload.
        foreach (var m in camp.Missions)
            if (HomeAutoBuild.OrbitKey(m) == oid)
                return Clone(m, d);
        var cm = new CampaignMission { Id = "own:" + oid, OrbitId = oid, BudgetKc = 1e12, ApoKm = o.apoKm, PeriKm = o.periKm, IncDeg = o.incDeg };
        if (o.periKm < 0) { cm.TargetKind = "apogee"; cm.PeriKm = 0; }
        else if (Math.Abs(o.apoKm - o.periKm) < 1) cm.TargetKind = "circular";
        else cm.TargetKind = "ellipse";
        cm.PayloadId = d.payloadType; cm.MassKg = d.payloadMass;
        if (d.payloadType != null && cat.PayloadById.TryGetValue(d.payloadType, out var pl)) cm.VolumeM3 = pl.volume;
        return cm;
    }

    static CampaignMission Clone(CampaignMission m, Design d)
    {
        // Same target and gates; payload, mass and budget follow the design so the own-orbit run
        // judges the trajectory, not the contract paperwork.
        var c = (CampaignMission)typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(m, null);
        c.Id = m.Id + "(own payload)"; c.PayloadId = d.payloadType; c.MassKg = d.payloadMass; c.BudgetKc = 1e12;
        if (d.payloadType != null && cat.PayloadById.TryGetValue(d.payloadType, out var pl)) c.VolumeM3 = pl.volume;
        c.DiameterM = 0;
        return c;
    }

    static Row RunCase(Case c)
    {
        var r = new Row { src = c.src, label = c.label };
        JsonObject json = JsonMut.CloneObject(c.design);
        CampaignMission mission = null;
        try
        {
            if (c.missionId != null)
            {
                mission = camp.ById(c.missionId);
                if (c.stamp) HomeAutoBuild.ApplyMission(cat, json, mission);
                r.kind = c.stamp ? "stamped" : "linked";
            }
        }
        catch (Exception e) { r.err += "stamp:" + e.GetType().Name + ":" + e.Message + ";"; }

        // ---- designer ----
        EngineeringReport rep = null;
        try { Readout.Reference(cat, json); } catch (Exception e) { r.err += "reference:" + e.GetType().Name + ";"; }
        try { rep = Validate.Engineering(cat, json); }
        catch (Exception e) { r.err += "validate:" + e.GetType().Name + ":" + e.Message + ";"; }
        if (rep != null)
        {
            r.ok = rep.ok;
            r.blocks = string.Join(",", rep.blocks.Select(b => b.code == "schema" || b.code == "payload-doesnt-fit" ? b.code + "(" + b.msg + ")" : b.code));
            r.warns = string.Join(",", rep.warnings.Select(b => b.code));
            var pres = Experience.Present(rep, "standard");
            r.ready = pres.ok && pres.next == Experience.Ready;
            r.verdict = !rep.ok ? "BLOCKED" : r.ready ? "READY" : "OK+warn";
            var ps = Experience.Present(rep, "simple");
            r.simple = !ps.ok ? "BLOCKED" : ps.next == Experience.Ready ? "READY" : "OK+warn";
            r.vPayloadFitBlock = rep.blocks.Any(b => b.code == "payload-doesnt-fit");
            var q = rep.readout;
            if (q != null)
            {
                r.dv = q.totalDeltaV; r.req = q.required; r.margin = q.margin; r.twr = q.liftoffTWR; r.dFairFits = q.fairingFits;
            }
        }
        else r.verdict = "ERROR";

        Design d;
        try { d = Design.FromJson(json); }
        catch (Exception e) { r.err += "fromjson:" + e.GetType().Name + ";"; Flag(r); return r; }
        if (mission == null) { mission = OwnMission(d); r.kind = "own"; }
        if (mission == null) { r.mission = "(no orbit)"; Flag(r); return r; }
        r.mission = mission.Id;
        r.mReq = mission.RequiredDeltaV();

        // ---- batch flights (campaign judge) ----
        var flights = new List<CampaignFlight>();
        foreach (var seed in camp.Seeds) flights.Add(FlightBatch.Fly(cat, mission, d, seed));
        r.passes = flights.Count(f => f.passed);
        r.outcomes = string.Join("/", flights.Select(f => f.outcome + (f.failure != null ? ":" + f.failure : "")).Distinct());
        var f1 = flights[0];
        r.apoKm = f1.apoKm; r.periKm = f1.periKm; r.idealDv = f1.idealDv;
        var g = new List<string>();
        if (!f1.payloadMatch) g.Add("payloadMismatch");
        if (!f1.fairingFits) g.Add("fairingNoFit");
        if (!f1.underBudget) g.Add("overBudget");
        if (f1.gentle) g.Add("gentleTier");
        if (f1.redRisk) g.Add("red:" + string.Join("+", f1.redKinds));
        if (mission.MarginNonNegative && f1.margin < 0) g.Add("marginNeg");
        if (!double.IsNaN(mission.ReliabilityMin) && f1.reliability < mission.ReliabilityMin) g.Add("reliability");
        if (!double.IsNaN(mission.MaxG) && f1.maxG > mission.MaxG) g.Add("maxG" + f1.maxG.ToString("0.0", Inv));
        if (mission.LandingRequired) g.Add("landing=" + (f1.landingGrade ?? "null"));
        if (mission.LandBoosters) g.Add("booster=" + (f1.boosterGrade ?? "null"));
        g.Add("crit=" + (f1.criterion ?? "none"));
        r.gates = string.Join(" ", g);
        foreach (var f in flights) if (f.outcome == "error") r.err += "fly:" + f.failure + ";";

        // determinism: same seed twice
        var again = FlightBatch.Fly(cat, mission, d, camp.Seeds[0]);
        if (!Same(f1, again, out var why)) { r.nondet = true; r.nondetWhy = "serial:" + why; }

        // ---- raw mirror of Fly for the SimResult ----
        try
        {
            var sim = RawFly(mission, d, camp.Seeds[0]);
            if (sim.outcome != f1.outcome) r.err += $"mirror-outcome {sim.outcome}!={f1.outcome};";
            r.dvExp = sim.deltaVExpended; r.lossG = sim.lossGravity; r.lossD = sim.lossDrag; r.lossS = sim.lossSteering; r.simT = sim.time;
            r.burnedOut = !sim.events.Any(e => e.type == "cutoff") && sim.events.Count(e => e.type == "burnout") >= Stack.SerialStages(cat, d).stages.Count;
            var st = Stack.SimInputs(cat, d).stages;
            var norm = Headless.Normalize(st);
            double m0 = st.Sum(s => s.propMass + s.dryMass) + Stack.SerialStages(cat, d).payload;
            r.simTwr = norm[0].mdot * Atmosphere.G0 * norm[0].ispSL / (m0 * Atmosphere.G0);
            if (Num(sim.orbit.apoAlt) || Num(sim.orbit.periAlt) || Num(sim.deltaVExpended)) r.err += "NaN-in-sim;";
        }
        catch (Exception e) { r.err += "raw:" + e.GetType().Name + ":" + e.Message + ";"; }

        // ---- pad emulation ----
        try { Live(mission, d, json, r); }
        catch (Exception e) { r.err += "live:" + e.GetType().Name + ":" + e.Message + ";"; }

        if (r.kind != "own" && rep != null)
        {
            try
            {
                var (fixedJ, changes) = Validate.MakeItFly(cat, json);
                r.mif = Repair(fixedJ, mission) + " {" + string.Join("; ", changes) + "}";
            }
            catch (Exception e) { r.mif = "EXC " + e.GetType().Name; r.err += "makeitfly:" + e.Message + ";"; }
            if (rep.readout != null && rep.readout.margin < 0)
            {
                try
                {
                    var fx = DvMarginFix.Solve(cat, json);
                    r.dvfix = fx == null ? "null" : fx.Solved && fx.Design != null ? Repair(fx.Design, mission) : "unsolved";
                }
                catch (Exception e) { r.dvfix = "EXC " + e.GetType().Name; r.err += "dvfix:" + e.Message + ";"; }
            }
        }
        if ((rep != null && rep.readout != null && (Num(r.dv) || Num(r.twr))) || Num(r.apoKm)) r.err += "NaN;";
        Flag(r);
        return r;
    }

    static string Repair(JsonObject j, CampaignMission mission)
    {
        string v;
        try { Readout.Reference(cat, j); } catch { }
        var rep = Validate.Engineering(cat, j);
        v = !rep.ok ? "BLOCKED[" + string.Join(",", rep.blocks.Select(b => b.code)) + "]" : "ok";
        var d = Design.FromJson(j);
        int pass = 0; string outc = "";
        foreach (var seed in camp.Seeds) { var f = FlightBatch.Fly(cat, mission, d, seed); if (f.passed) pass++; outc = f.outcome + (f.failure != null ? ":" + f.failure : ""); }
        return v + " batch " + pass + "/5 " + outc;
    }

    static bool Num(double x) => double.IsNaN(x) || double.IsInfinity(x);

    static bool Same(CampaignFlight a, CampaignFlight b, out string why)
    {
        why = "";
        if (a.outcome != b.outcome) why += "outcome ";
        if (a.passed != b.passed) why += "passed ";
        if (!Eq(a.apoKm, b.apoKm)) why += "apo ";
        if (!Eq(a.periKm, b.periKm)) why += "peri ";
        if (!Eq(a.maxG, b.maxG)) why += "maxG ";
        if (a.failure != b.failure) why += "failure ";
        return why == "";
    }

    static bool Eq(double a, double b) => a.Equals(b);

    /// <summary>FlightBatch.Fly lines 117-166, returning the SimResult.</summary>
    static SimResult RawFly(CampaignMission mission, Design design, ulong seed)
    {
        var flown = design.Clone();
        if (mission.CoastHours > 0)
        {
            var upper = flown.stages[flown.stages.Count - 1];
            upper.propMass = TankModel.RemainingPropKg(cat, upper, mission.CoastHours);
        }
        if (mission.LandBoosters && flown.strapOns != null && flown.strapOns.landingPropMass > 0)
            flown.strapOns.propMass -= Math.Min(flown.strapOns.landingPropMass, flown.strapOns.propMass);
        var built = Stack.SimInputs(cat, flown, 0.2, 0);
        double target = mission.GuidanceAltM;
        if (!(target > 0)) target = built.opts.targetAlt;
        built.opts.targetAlt = target;
        if (built.opts.guidance != null) built.opts.guidance.targetAlt = target;
        if (mission.TargetKind == "circular" || mission.TargetKind == "ellipse" || mission.TargetKind == "c3")
        {
            bool custom = built.opts.guidance != null && !Headless.LegacyPitch(built.opts.guidance);
            if (custom) { built.opts.cutoff = true; built.opts.targetAlt = 1e12; built.opts.guidance.targetAlt = target; }
            else { built.opts.cutoff = false; built.opts.targetAlt = target; }
        }
        var monitor = FailureMonitor.Create(cat, flown, target, FlightBatch.DesignHash(design), seed);
        monitor.Gentle = false;
        built.opts.monitor = monitor;
        return Headless.Simulate(built.stages, built.opts);
    }

    /// <summary>FlightDriver.Launch + AscentFlight.Prepare + PostFlightSource.Facts, without the director, wind or site latitude.</summary>
    static void Live(CampaignMission mission, Design d, JsonObject json, Row r)
    {
        var (stages, opts) = Stack.SimInputs(cat, d, 1.0 / 120);
        ulong hash = FlightBatch.DesignHash(d);
        double target = Stack.RequiredDeltaV(cat, d.payloadOrbit).targetAlt;
        if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
        if (FailureMonitor.Engages(d)) opts.monitor = FailureMonitor.Create(cat, d, target, hash, 1);
        MissionAim.Apply(opts, mission);
        opts.tMax = LiveTMax;
        var sim = new AscentSim(stages, opts);
        // FlightDriver.Launch + StopForBreakup (played breakups)
        bool arcade = PlayedBreakup.Arcade(d);
        double qLimPa = FailureMonitor.StructuralQLimPa(cat, d);
        double liftoffTwr = stages.Count > 0 && sim.Y[4] > 1 ? stages[0].thrustSL / (sim.Y[4] * Atmosphere.G0) : 0;
        double ideal = Headless.StackDeltaV(stages, opts.payload).total;
        double dvMargin = PlayedBreakup.Margin(ideal, Stack.RequiredDeltaV(cat, d.payloadOrbit).required);
        var serial = Stack.SerialStages(cat, d).stages;
        int mecoStage = serial.FindIndex(s => s.role == "core" && s.index == 0) + 1;
        bool meco = false; string played = null;
        while (!sim.Done)
        {
            int before = sim.Events.Count;
            sim.Tick();
            for (int i = before; i < sim.Events.Count; i++) if (sim.Events[i].type == "burnout" && sim.Events[i].stage == mecoStage) meco = true;
            FailureEvent ev = null; bool stop = false;
            if (sim.Events.Any(e => e.type == "failure")) { sim.BreakUp(null); stop = true; }
            else
            {
                double alt = Math.Sqrt(sim.Y[0] * sim.Y[0] + sim.Y[1] * sim.Y[1]) - Gravity.R_EARTH;
                if (sim.Events.Any(e => e.type == "impact")) ev = PlayedBreakup.Impact(sim.T);
                else if (meco && dvMargin < 0) ev = PlayedBreakup.Short(sim.T, dvMargin);
                else if (arcade)
                {
                    double mass = sim.Y[4];
                    var sample = new PlayedBreakup.Sample { T = sim.T, AltM = alt, QPa = sim.LastQ, AccelMps2 = mass > 1 ? sim.LastThrust / mass : 0, QLimPa = qLimPa, LiftoffTwr = liftoffTwr, Thrusting = sim.Thrusting };
                    ev = PlayedBreakup.At(sample);
                }
                if (ev != null) { sim.BreakUp(ev); played = ev.cause; stop = true; }
            }
            if (stop) break;
        }
        var res = sim.Finish();
        if (played != null) r.live = "played:" + played + " ";
        bool intact = res.failure == null && res.outcome != "failure" && res.outcome != "crash";
        var extra = new GoalCheck { IdealDv = res.idealDeltaV, C3Km2PerS2 = MissionScore.C3(res.orbit.energy) / 1e6 };
        double req = mission.RequiredDeltaV();
        extra.Margin = req > 1 ? (res.idealDeltaV - req) / req : 0;
        if (res.risks != null) { extra.RedKnown = true; extra.RedRisk = res.risks.Any(x => x.level == "red"); }
        extra.Reliability = Reliability.Of(cat, d).reliability;
        extra.Gentle = FailureMonitor.GentleTier(d);
        extra.PayloadMatch = d.payloadType == mission.PayloadId && d.payloadMass == mission.MassKg;
        extra.FairingFits = PayloadFit.Check(d, new PayloadSpec { id = mission.PayloadId, volume = mission.VolumeM3, diameter = mission.DiameterM }).fits;
        var price = CostModel.PriceDesign(cat, d); long per = CostModel.MinorPerKc(cat);
        extra.UnderBudget = price.recurring <= Career.KcToMinor(mission.BudgetKc, per > 0 ? per : 1);
        extra.AirframeKnown = true;
        if (mission.CoastHours > 0) { var up = d.stages[d.stages.Count - 1]; extra.BoilOffKg = up.propMass - TankModel.RemainingPropKg(cat, up, mission.CoastHours); }
        string landing = null;
        if (mission.LandingRequired || mission.LandBoosters) landing = FlightBatch.PredictLandingGrade(cat, d, res, mission.LandingMode, false);
        if (mission.LandBoosters) { extra.BoosterKnown = true; extra.BoosterGrade = FlightBatch.PredictLandingGrade(cat, d, res, mission.LandingMode, true); }
        r.livePass = FlightDebrief.GoalMet(mission, intact, res.orbit.apoAlt, res.orbit.periAlt, res.maxG, landing, extra);
        r.live += res.outcome + (res.failure != null ? ":" + res.failure.kind : "") + "@" + res.time.ToString("0", Inv) + "s";
        r.liveApo = res.orbit.apoAlt / 1000; r.livePeri = res.orbit.periAlt / 1000;
    }

    static void Flag(Row r)
    {
        bool designerSaysEnough = r.ok && !(r.margin < 0);
        if (r.passes == 0 && r.ready) r.flags.Add("READY_BUT_FAILS");
        if (r.passes == 0 && r.simple == "READY") r.flags.Add("SIMPLE_LAYER_READY_BUT_FAILS");
        if (r.mif.Contains("batch 0/5") && r.passes > 0) r.flags.Add("MAKEITFLY_BREAKS_A_FLYER");
        if (r.mif.StartsWith("ok batch 0/5")) r.flags.Add("MAKEITFLY_OK_BUT_FAILS");
        if (r.dvfix.StartsWith("ok batch 0/5")) r.flags.Add("DVFIX_OK_BUT_FAILS");
        else if (r.passes == 0 && designerSaysEnough) r.flags.Add("OK_MARGIN_BUT_FAILS");
        if (!r.ok && r.verdict == "BLOCKED" && r.passes == 5) r.flags.Add("BLOCKED_BUT_FLIES");
        else if (!r.ok && r.verdict == "BLOCKED" && r.passes > 0) r.flags.Add("BLOCKED_BUT_SOME_SEEDS_FLY");
        if (r.ok && r.margin < 0 && r.passes == 5) r.flags.Add("DV_SHORT_BUT_FLIES");
        if (!Num(r.dv) && !Num(r.idealDv) && r.dv > 0 && Math.Abs(r.idealDv - r.dv) / r.dv > 0.01) r.flags.Add($"DV_DESIGNER_VS_FLIGHT_IDEAL({r.dv:0}->{r.idealDv:0})");
        if (!Num(r.req) && !Num(r.mReq) && r.mReq > 0 && Math.Abs(r.req - r.mReq) / r.mReq > 0.01) r.flags.Add($"REQ_DESIGNER_VS_MISSION({r.req:0} vs {r.mReq:0})");
        if (r.burnedOut && !Num(r.dvExp) && r.dv > 0 && Math.Abs(r.dvExp - r.dv) / r.dv > 0.10) r.flags.Add($"DV_EXPENDED_OFF>10%({r.dvExp:0}/{r.dv:0})");
        if (!Num(r.twr) && !Num(r.simTwr) && r.twr > 0 && Math.Abs(r.simTwr - r.twr) / r.twr > 0.02) r.flags.Add($"TWR_SHOWN_VS_SIM({r.twr:0.000}/{r.simTwr:0.000})");
        if (r.dFairFits.HasValue && r.dFairFits.Value && r.gates != null && r.gates.Contains("fairingNoFit")) r.flags.Add("FAIRING_DESIGNER_FITS_FLIGHT_NOT");
        if (r.passes >= 1 && r.live != "" && !r.livePass) r.flags.Add("BATCH_PASS_LIVE_FAIL");
        if (r.passes == 0 && r.live != "" && r.livePass) r.flags.Add("BATCH_FAIL_LIVE_PASS");
        if (r.live.StartsWith("timeout")) r.flags.Add("LIVE_TIMEOUT");
        if (r.err != "") r.flags.Add("ERROR");
        if (r.nondet) r.flags.Add("NONDETERMINISTIC");
    }

    static void ParallelDeterminism(List<Case> cases, List<Row> rows)
    {
        // Same seeds through Parallel.For (FlightBatch.Simulate runs >= 8 flights this way).
        var idx = Enumerable.Range(0, rows.Count).Where(i => rows[i].passes >= 0 && rows[i].kind != "own").ToList();
        var a = new CampaignFlight[idx.Count];
        var designs = idx.Select(i => { var j = JsonMut.CloneObject(cases[i].design); var m = camp.ById(cases[i].missionId); if (cases[i].stamp) HomeAutoBuild.ApplyMission(cat, j, m); return (Design.FromJson(j), m); }).ToList();
        for (int rep = 0; rep < 2; rep++)
        {
            Parallel.For(0, idx.Count, k => a[k] = FlightBatch.Fly(cat, designs[k].Item2, designs[k].Item1, camp.Seeds[0]));
            for (int k = 0; k < idx.Count; k++)
            {
                var serial = FlightBatch.Fly(cat, designs[k].Item2, designs[k].Item1, camp.Seeds[0]);
                if (!Same(serial, a[k], out var why))
                {
                    var r = rows[idx[k]];
                    r.nondet = true; r.nondetWhy += " parallel" + rep + ":" + why;
                    if (!r.flags.Contains("NONDETERMINISTIC")) r.flags.Add("NONDETERMINISTIC");
                }
            }
        }
    }

    /// <summary>Upper-stage propellant ±k% on each campaign winner: designer margin vs batch passes.</summary>
    static void Sensitivity(string root)
    {
        var g = MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
        foreach (JsonObject m in g.Arr("missions"))
        {
            var mission = camp.ById(m.Str("id"));
            var d0 = (JsonObject)m.Arr("designs")[0];
            var line = new StringBuilder(mission.Id + " " + mission.TargetKind + ":");
            foreach (var k in new[] { -0.10, -0.05, -0.02, 0, 0.02, 0.05, 0.10, 0.20 })
            {
                var j = JsonMut.CloneObject(d0);
                var st = j.Arr("stages"); var up = (JsonObject)st[st.Count - 1];
                JsonMut.Set(up, "propMass", up.Num("propMass") * (1 + k));
                var q = Readout.Quick(cat, j);
                var d = Design.FromJson(j);
                int pass = 0; CampaignFlight f1 = null;
                foreach (var seed in camp.Seeds) { var f = FlightBatch.Fly(cat, mission, d, seed); if (f.passed) pass++; if (f1 == null) f1 = f; }
                line.Append($"  [{k:+0%;-0%;0%} m{q.margin * 100:0.0}% {pass}/5 {f1.apoKm:0}x{f1.periKm:0}]");
            }
            Console.WriteLine(line);
        }
    }

    static void Show(string what, JsonObject j, CampaignMission m)
    {
        try { Readout.Reference(cat, j); } catch { }
        var rep = Validate.Engineering(cat, j);
        var q = rep.readout;
        string v = !rep.ok ? "BLOCKED[" + string.Join(",", rep.blocks.Select(b => b.code + (b.code == "schema" || b.code=="payload-doesnt-fit" ? "(" + b.msg + ")" : ""))) + "]" : (Experience.Present(rep, "standard").next == Experience.Ready ? "READY" : "OK w[" + string.Join(",", rep.warnings.Select(b => b.code)) + "]");
        string simple = Experience.Present(rep, "simple").next;
        var d = Design.FromJson(j);
        int pass = 0; CampaignFlight f1 = null;
        foreach (var seed in camp.Seeds) { var f = FlightBatch.Fly(cat, m, d, seed); if (f.passed) pass++; if (f1 == null) f1 = f; }
        Console.WriteLine($"{what}: designer {v} simple=\"{simple}\" dv {q?.totalDeltaV:0} req {q?.required:0} twr {q?.liftoffTWR:0.000} fits(readout) {q?.fairingFits} | batch {pass}/5 {f1.outcome}{(f1.failure != null ? ":" + f1.failure : "")} apo {f1.apoKm:0.0} peri {f1.periKm:0.0} payloadMatch {f1.payloadMatch} fairingFits {f1.fairingFits} under {f1.underBudget}");
    }

    static void Probe(string root)
    {
        var g = MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
        JsonObject W(string id) { foreach (JsonObject m in g.Arr("missions")) if (m.Str("id") == id) return JsonMut.CloneObject((JsonObject)m.Arr("designs")[0]); return null; }
        // 1. fairing: m5 winner with a shorter fairing
        var a = W("m5"); Show("m5 winner as-is", a, camp.ById("m5"));
        JsonMut.Set(a.Obj("fairing"), "length", 3.0); Show("m5 winner fairing L=3.0", a, camp.ById("m5"));
        // 2. m3: 20 kg cubesat vs payload.mass min 50
        var b = W("m3"); Show("m3 winner (20 kg)", b, camp.ById("m3"));
        JsonMut.Set(b.Obj("payload"), "mass", 50.0); Show("m3 winner payload raised to 50 kg (schema min)", b, camp.ById("m3"));
        // 3. m1: winner (designer dv short) and +10% prop
        var c = W("m1"); Show("m1 winner", c, camp.ById("m1"));
        foreach (var id in new[] { "m6", "m7", "m10", "m19" })
        {
            var j = W(id); var m = camp.ById(id);
            var dd = Design.FromJson(j);
            var rq = Readout.Reference(cat, j);
            var rsim = ReferenceAscent.Simulate(cat, j);
            var bos = rsim.events.Where(e => e.type == "burnout").Select(e => $"S{e.stage}@{e.v:0}").ToArray();
            int coreSerial = dd.StrapOnCount > 0 ? 2 : 1;
            var coreEv = rsim.events.FirstOrDefault(e => e.type == "burnout" && e.stage == coreSerial);
            string mode = dd.recoveryMode ?? m.LandingMode;
            double needCore = coreEv != null ? RecoveryModel.BoostbackDeltaV(mode, coreEv.v.Value) : double.NaN;
            Console.WriteLine($"landing {id}: straps {dd.StrapOnCount} recoveryMode {dd.recoveryMode ?? "null"} (mission {m.LandingMode}) | readout landingNeed {rq.landingNeed_mps?.ToString("0") ?? "null"} reserve {rq.landingReserve_mps:0} | burnouts {string.Join(" ", bos)} | need at core burnout (FlightBatch basis) {needCore:0}");
        }
        // 4. TWR basis: one gg-kerolox-heavy, prop tuned so catalog TWR ~1.02
        var d0 = W("m4");
        var st = d0.Arr("stages"); var s0 = (JsonObject)st[0];
        JsonMut.Set(s0, "engine", "gg-kerolox-heavy"); JsonMut.Set(s0, "engineCount", 1.0); JsonMut.Set(s0, "propellant", "kerolox"); JsonMut.Set(s0, "nozzle", "default");
        if (s0.ContainsKey("engineParams")) JsonMut.Set(s0, "engineParams", new JsonObject());
        if (s0.ContainsKey("mixtureOffsetPct")) JsonMut.Set(s0, "mixtureOffsetPct", 0.0);
        for (int it = 0; it < 60; it++)
        {
            var q = Readout.Quick(cat, d0);
            double want = 1.02, k = q.liftoffTWR / want;
            JsonMut.Set(s0, "propMass", s0.Num("propMass") * (1 + (k - 1) * 0.9));
        }
        var q2 = Readout.Quick(cat, d0);
        var (stg, opts) = Stack.SimInputs(cat, Design.FromJson(d0));
        var n0 = Headless.Normalize(stg)[0];
        double m0 = Headless.Normalize(stg).Sum(x => x.propMass + x.dryMass) + opts.payload;
        Console.WriteLine($"twr probe: designer liftoff TWR {q2.liftoffTWR:0.000} (thrustSL basis); sim liftoff thrust {n0.mdot * Atmosphere.G0 * n0.ispSL:0} N vs catalog {q2.stages[0].twr * q2.stages[0].m0 * Atmosphere.G0:0} N -> sim TWR {n0.mdot * n0.ispSL / m0:0.000}");
        var sim = Headless.Simulate(stg, opts);
        var lift = sim.events.FirstOrDefault(e => e.type == "liftoff");
        Console.WriteLine($"   sim liftoff at T+{(lift != null ? lift.t.ToString("0.0") : "never")} s, outcome {sim.outcome}; designer blocks: {string.Join(",", Validate.Engineering(cat, d0).blocks.Select(x => x.code))}");
    }

    static string F(double x, string fmt) => Num(x) ? "-" : x.ToString(fmt, Inv);

    static void Write(string outDir, List<Row> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("src\tlabel\tkind\tmission\tverdict(simple)\tblocks\twarnings\tdesignerDv\tdesignerReq\tmissionReq\tmargin%\tliftoffTWR\tsimTWR\tbatchPasses/5\tbatchOutcomes\tapoKm\tperiKm\tflightIdealDv\tdvExpended\tlossG\tlossD\tlive(pad-emu)\tlivePass\tliveApo\tlivePeri\tgates\tflags\terr\tnondet\tmakeItFly\tdvMarginFix");
        foreach (var r in rows)
            sb.AppendLine(string.Join("\t", r.src, r.label, r.kind, r.mission, r.verdict + "(" + r.simple + ")", r.blocks, r.warns, F(r.dv, "0"), F(r.req, "0"), F(r.mReq, "0"), F(r.margin * 100, "0.0"), F(r.twr, "0.000"), F(r.simTwr, "0.000"),
                r.passes, r.outcomes, F(r.apoKm, "0.0"), F(r.periKm, "0.0"), F(r.idealDv, "0"), F(r.dvExp, "0"), F(r.lossG, "0"), F(r.lossD, "0") + "|t" + F(r.simT, "0"), r.live, r.livePass, F(r.liveApo, "0.0"), F(r.livePeri, "0.0"), r.gates, string.Join(" ", r.flags), r.err, r.nondetWhy, r.mif, r.dvfix));
        File.WriteAllText(Path.Combine(outDir, "results.tsv"), sb.ToString());

        var s = new StringBuilder();
        s.AppendLine($"rows {rows.Count}");
        foreach (var grp in rows.SelectMany(r => r.flags.Select(f => (f.Split('(')[0], r))).GroupBy(x => x.Item1).OrderBy(x => x.Key))
        {
            s.AppendLine($"== {grp.Key}: {grp.Count()}");
            foreach (var (_, r) in grp.Take(400))
                s.AppendLine($"   {r.label} | {r.mission} | {r.verdict} [{r.blocks}] w[{r.warns}] dv {F(r.dv, "0")} req {F(r.req, "0")}/{F(r.mReq, "0")} twr {F(r.twr, "0.00")} | batch {r.passes}/5 {r.outcomes} apo {F(r.apoKm, "0")} peri {F(r.periKm, "0")} | live {r.live} {(r.livePass ? "PASS" : "fail")} | {r.gates} | mif {r.mif} | dvfix {r.dvfix} | simple {r.simple} | {string.Join(" ", r.flags.Where(f => f.Split('(')[0] == grp.Key))} {r.err} {r.nondetWhy}");
        }
        File.WriteAllText(Path.Combine(outDir, "summary.txt"), s.ToString());
        Console.WriteLine(s.ToString());
    }
}

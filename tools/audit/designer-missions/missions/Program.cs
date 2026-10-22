using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using Kestrel.Core.Designer;
using Kestrel.Core.Physics;
using Kestrel.Core.Economy;

static class P
{
    static readonly string Root = System.Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT") ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../../.."));
    static Catalog cat;
    static Campaign camp;

    static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        cat = Catalog.LoadFromRepo(Root);
        camp = Campaign.Load(Root);
        HomeAutoBuild.InstallMissionOrbits(cat, camp);
        long perKc = CostModel.MinorPerKc(cat);
        Console.WriteLine("catalog minorPerKc=" + perKc);
        if (args.Length > 0 && args[0] == "fit") { Fit(); return; }
        if (args.Length > 0 && args[0] == "board") { Board(); return; }
        if (args.Length > 0 && args[0] == "credits") { Credits(); return; }
        string file = args.Length > 0 ? args[0] : "campaign-winners";
        bool live = args.Length > 1 && args[1] == "live";
        var golden = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root, "data", "golden", file + ".json")));
        var budgets = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root, "data", "designer-budget.json")));
        foreach (JsonObject block in golden.Arr("missions"))
        {
            var m = camp.ById(block.Str("id"));
            foreach (JsonObject raw in block.Arr("designs"))
            {
                var json = JsonMut.CloneObject(raw);
                string rawOrbit = raw.Obj("payload")?.Str("orbit");
                bool hadBudget = raw.ContainsKey("budgetKc");
                HomeAutoBuild.ApplyMission(cat, json, m);
                var d = Design.FromJson(json);
                // batch
                int bp = 0; string bfail = ""; double costKc = 0, margin = 0, maxG = 0; int stars = 0; double score = 0; string lg = "", bg = "";
                foreach (var seed in camp.Seeds)
                {
                    var f = FlightBatch.Fly(cat, m, Design.FromJson(json), seed);
                    if (f.passed) bp++; else bfail = f.outcome + "/" + f.failure + " apo=" + f.apoKm.ToString("0") + " peri=" + f.periKm.ToString("0") + " c3=" + f.c3Km2PerS2.ToString("0.00");
                    costKc = f.costKc; margin = f.margin; maxG = Math.Max(maxG, f.maxG); stars = f.stars; score = f.score; lg = f.landingGrade; bg = f.boosterGrade;
                }
                // designer
                var rep = Validate.Engineering(cat, json);
                var r = rep.readout;
                string blocks = string.Join(",", rep.blocks.Select(b => b.code + (b.code=="schema"||b.code=="payload-doesnt-fit" ? ":"+b.msg : "")));
                string warns = string.Join(",", rep.warnings.Select(b => b.code));
                var fitMission = PayloadFit.Check(d, new PayloadSpec { volume = m.VolumeM3, diameter = m.DiameterM });
                double designerVol = Validate.PayloadVolume(cat, json);
                double designerCap = Math.PI * Math.Pow(d.fairingDiameter / 2, 2) * d.fairingLength * 0.6;
                double rawBudget = 0;
                var by = budgets.Obj("byOrbit");
                rawBudget = rawOrbit != null && by.ContainsKey(rawOrbit) ? by.Num(rawOrbit) : budgets.Num("defaultKc");
                Console.WriteLine($"{m.Id,-4} {raw.Str("id"),-26} batch {bp}/5 {bfail} | cost {costKc:0} Kc / budget {m.BudgetKc:0} ({100 * costKc / m.BudgetKc:0.000}%) hangarFallback({rawOrbit})={rawBudget} | margin {margin:0.000} maxG {maxG:0.00} stars {stars} score {score:0} land {lg}/{bg}");
                Console.WriteLine($"      designer ok={rep.ok} blocks=[{blocks}] warns=[{warns}] reqDv={(r != null ? r.required : 0):0} dv={(r != null ? r.totalDeltaV : 0):0} dmargin={(r != null ? r.margin : 0):0.000} twr={(r != null ? r.liftoffTWR : 0):0.00} | fit mission={fitMission.fits} (volM {fitMission.volumeMargin:0.0} diaM {fitMission.diameterMargin:0.00}) designerNeeds {designerVol:0.0} has {designerCap:0.0} fairing {d.fairingDiameter}x{d.fairingLength} | rel={Reliability.Of(cat, d).reliability:0.0000}");
                if (live) Live(m, d);
            }
        }
    }

    static void Board()
    {
        string T(string n) => File.ReadAllText(Path.Combine(Root, "data", n + ".json"));
        var hangar = Kestrel.Core.Hangar.HangarData.FromJson(T("designer-templates"), T("designer-look"), T("designer-budget"), T("finishes"), T("liveries"));
        foreach (var m in camp.Missions.Where(x => Environment.GetEnvironmentVariable("ONLY") == null || Environment.GetEnvironmentVariable("ONLY").Split(',').Contains(x.Id)))
        {
            var choice = HomeAutoBuild.Choose(cat, hangar.templates, m);
            var json = choice.Design;
            var rep = Validate.Engineering(cat, json);
            var d = Design.FromJson(json);
            int bp = 0; string why = "";
            foreach (var seed in camp.Seeds) { var f = FlightBatch.Fly(cat, m, Design.FromJson(json), seed); if (f.passed) bp++; else why = (f.failure ?? f.outcome) + $" apo={f.apoKm:0} peri={f.periKm:0} pm={f.payloadMatch} fit={f.fairingFits} land={f.landingGrade}/{f.boosterGrade}"; }
            double hb = hangar.budgets.For(json);
            Console.WriteLine($"{m.Id,-4} tpl={choice.TemplateId} name={json.Str("name")} launchEnabled={choice.LaunchEnabled} blocks=[{string.Join(",", rep.blocks.Select(b => b.code + ":" + b.msg))}] payload={d.payloadType}/{d.payloadMass} orbit={d.payloadOrbit} hangarBudget={hb} reach='{choice.ReachLine}' batch {bp}/5 {why}");
            Live(m, d);
        }
    }

    static void Credits()
    {
        var table = CreditsTable.Parse(File.ReadAllText(Path.Combine(Root, "data", "credits.json")));
        var golden = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root, "data", "golden", "campaign-winners.json")));
        long earned = table.Starting;
        foreach (JsonObject block in golden.Arr("missions"))
        {
            var m = camp.ById(block.Str("id"));
            var raw = (JsonObject)block.Arr("designs")[0];
            var json = JsonMut.CloneObject(raw);
            HomeAutoBuild.ApplyMission(cat, json, m);
            Console.WriteLine($"{m.Id,-4} winnerQuote={table.Quote(json)} earnedBefore(no medals)={earned}");
            earned += table.MissionClear(m.Index) + table.FirstClear;
        }
    }

    static void Fit()
    {
        var golden = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Root, "data", "golden", "campaign-winners.json")));
        foreach (JsonObject block in golden.Arr("missions"))
        {
            var m = camp.ById(block.Str("id"));
            var raw = (JsonObject)block.Arr("designs")[0];
            // scan fairing sizes: find any where designer passes fit but mission fails, and vice versa
            string a = "", b = "";
            foreach (var D in new[] { 1.0, 1.6, 2.0, 2.4, 3.0, 3.2, 3.6, 3.7, 3.8, 4.0, 4.2, 5.0 })
                foreach (var L in new[] { 2.0, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0, 12.0, 15.0 })
                {
                    var json = JsonMut.CloneObject(raw);
                    HomeAutoBuild.ApplyMission(cat, json, m);
                    JsonMut.Set(json.Obj("fairing"), "diameter", D);
                    JsonMut.Set(json.Obj("fairing"), "length", L);
                    var rep = Validate.Engineering(cat, json);
                    bool dfit = !rep.blocks.Any(x => x.code == "payload-doesnt-fit");
                    var d = Design.FromJson(json);
                    bool mfit = PayloadFit.Check(d, new PayloadSpec { volume = m.VolumeM3, diameter = m.DiameterM }).fits;
                    if (dfit && !mfit && a == "") { a = $"{D}x{L}"; var f = FlightBatch.Fly(cat, m, d, 1); a += $" (batch passed={f.passed} fairingFits={f.fairingFits} designerOk={rep.ok} blocks={string.Join(",", rep.blocks.Select(x=>x.code))})"; }
                    if (!dfit && mfit && b == "") b = $"{D}x{L}";
                }
            Console.WriteLine($"{m.Id,-4} payload {m.PayloadId} mission vol {m.VolumeM3} dia {m.DiameterM} | designer-OK-but-mission-fails: {a} | designer-blocks-but-mission-fits: {b}");
        }
    }

    static void Live(CampaignMission m, Design d)
    {
        ulong hash = FlightBatch.DesignHash(d);
        string lines = "";
        foreach (var variant in (Environment.GetEnvironmentVariable("VARS") ?? "full,lat28-calm-t600,site-t2500,full-dt0.2").Split(','))
        {
            int pass = 0; string why = "";
            foreach (var seed in camp.Seeds)
            {
                double dt = variant == "full-dt0.2" ? 0.2 : 1.0 / 120;
                var (stages, opts) = Stack.SimInputs(cat, d, dt);
                double target = Stack.RequiredDeltaV(cat, d.payloadOrbit).targetAlt;
                if (MissionAim.IsApogeeHop(m)) target = m.GuidanceAltM;
                if (FailureMonitor.Engages(d)) opts.monitor = FailureMonitor.Create(cat, d, target, hash, seed);
                var card = MissionBriefing.Load(Root, m.Index, hash, seed);
                if (!variant.Contains("lat28"))
                {
                    if (card.Wind != null && !card.Wind.Calm) opts.wind = card.Wind;
                    if (card.Site != null) opts.latitude = card.Site.LatitudeDeg;
                }
                MissionAim.Apply(opts, m);
                opts.tMax = variant.Contains("t2500") ? 2500 : 600;
                var res = Headless.Simulate(stages, opts);
                bool intact = res.failure == null && res.outcome != "failure" && res.outcome != "crash";
                double req = m.RequiredDeltaV();
                var extra = new GoalCheck
                {
                    IdealDv = res.idealDeltaV,
                    C3Km2PerS2 = MissionScore.C3(res.orbit.energy) / 1e6,
                    Margin = req > 1 ? (res.idealDeltaV - req) / req : 0,
                };
                if (res.risks != null) { extra.RedKnown = true; foreach (var rk in res.risks) if (rk.level == "red") extra.RedRisk = true; }
                extra.Reliability = Reliability.Of(cat, d).reliability;
                extra.Gentle = FailureMonitor.GentleTier(d);
                extra.PayloadMatch = d.payloadType == m.PayloadId && d.payloadMass == m.MassKg;
                extra.FairingFits = PayloadFit.Check(d, new PayloadSpec { volume = m.VolumeM3, diameter = m.DiameterM }).fits;
                var price = CostModel.PriceDesign(cat, d);
                extra.UnderBudget = price.recurring <= Career.KcToMinor(m.BudgetKc, CostModel.MinorPerKc(cat));
                extra.AirframeKnown = true;
                if (m.CoastHours > 0) { var up = d.stages[d.stages.Count - 1]; extra.BoilOffKg = up.propMass - TankModel.RemainingPropKg(cat, up, m.CoastHours); }
                string lgr = null;
                if (m.LandingRequired || m.LandBoosters) lgr = FlightBatch.PredictLandingGrade(cat, d, res, m.LandingMode, false);
                if (m.LandBoosters) { extra.BoosterKnown = true; extra.BoosterGrade = FlightBatch.PredictLandingGrade(cat, d, res, m.LandingMode, true); }
                bool ok = FlightDebrief.GoalMet(m, intact, res.orbit.apoAlt, res.orbit.periAlt, res.maxG, lgr, extra);
                if (ok) pass++;
                else why = $"s{seed}@{(card.Site != null ? card.Site.Id : "?")}:{res.outcome}/{res.failure?.kind} t={res.time:0} apo={res.orbit.apoAlt / 1000:0} peri={res.orbit.periAlt / 1000:0.00} g={res.maxG:0.00} land={lgr}/{extra.BoosterGrade} red={extra.RedRisk}";
            }
            lines += $" [{variant} {pass}/5 {why}]";
        }
        Console.WriteLine("      live:" + lines);
    }
}

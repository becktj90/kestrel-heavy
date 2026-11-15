// Writes data/starter-kicks.json (DECISIONS #85: the Starter screen is forgiving). For every campaign mission and every
// non-default Starter setting (engine fuel x stages x fuel notch x boosters, as StarterRocket offers them) it searches the
// stage-1 pitch kick, the one guidance value the Starter flight picks per setting. The upper stage keeps its #313 energy
// cutoff at the mission's target energy for every kick.
//
// Search, deterministic and serial:
//  1. Build the setting with the base rocket's kick (StarterKicks.Disabled), as StarterRocket.Build replays it.
//  2. If its designer practice flight (ReferenceAscent.Flight: seed-1 briefing wind and pad, BatchDt, played checks,
//     judged by StarterVerdict.Judge) and its live pad flight (LiveDt physics in 1/60 s frames, the same judge) both pass,
//     keep the base kick (no row).
//  3. Else fly the practice flight at every kick on a grid (KickMin..KickMax, KickStep). Take the widest run of passing kicks
//     (ties: nearer the base kick) and pick its middle, so a small live/batch difference stays inside the run. Fly the live
//     pad there; if it misses, try the run's other kicks outward from the middle, then the other runs.
//  4. No kick that passes both: no row. The setting keeps the base kick and the predictor names its cause before launch.
// Nothing about the physics changes: a kick is a design value inside designer-limits (guidance.pitchKickDeg 0..30).
//
// Usage (from the repo root):  dotnet run -c Release --project tools/starter-kicks [-- m3 m5 ...]
// With mission ids only those rows are recomputed; the others are kept from the existing file.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace StarterKicksTool
{
    static class Program
    {
        const double KickMin = 0, KickMax = 20, KickStep = 0.25;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static Catalog catalog;

        static int Main(string[] args)
        {
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            catalog = Catalog.LoadFromRepo(root);
            var campaign = Campaign.Load(root);
            StarterBases.Active = StarterBases.Parse(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-winners.json")));
            StarterReadinessCopy.Active = StarterReadinessCopy.Load(root);
            if (args != null && args.Length == 3 && args[0] == "probe") return Probe(campaign.ById(args[1]), args[2]);
            string outPath = Path.Combine(root, "data", StarterKicks.FileName);
            var only = new HashSet<string>();
            bool fitGuidance = false;
            for (int a = 0; args != null && a < args.Length; a++)
            {
                if (args[a] == "--guidance-computer") fitGuidance = true;
                else if (args[a] == "--out" && a + 1 < args.Length) outPath = Path.GetFullPath(args[++a]);
                else only.Add(args[a]);
            }
            var old = File.Exists(outPath) ? MiniJson.ParseObject(File.ReadAllText(outPath)).Obj("missions") : null;

            StarterKicks.Disabled = true;
            var rows = new List<string>();
            var total = Stopwatch.StartNew();
            foreach (var mission in campaign.Missions)
            {
                if (only.Count > 0 && !only.Contains(mission.Id))
                {
                    if (old?.Obj(mission.Id) is JsonObject keep) rows.Add(Row(mission.Id, keep.Num("baseKickDeg", double.NaN), Picks(keep.Obj("kicks"))));
                    continue;
                }
                var sw = Stopwatch.StartNew();
                var rocket = new StarterRocket(catalog, mission, StarterBases.For(mission.Id));
                double baseKick = StarterKicks.KickOf(rocket.BuildDesign(rocket.Default));
                var picked = new List<Pick>();
                int settings = 0, kept = 0, none = 0, guided = 0;
                foreach (var c in Settings(rocket))
                {
                    if (c.Equals(rocket.Default)) continue;
                    var json = rocket.BuildDesign(c);
                    var check = StarterRocket.CheckDesign(catalog, json, mission);
                    if (check.Blocked) continue;
                    settings++;
                    if (Wins(mission, json, check.FairingFits)) { kept++; continue; }
                    double k = Search(mission, json, check.FairingFits, baseKick);
                    bool gc = false;
                    if (double.IsNaN(k) && fitGuidance && !StarterKicks.HasGuidanceComputer(json))
                    {
                        var withGc = JsonMut.CloneObject(json);
                        StarterKicks.FitGuidanceComputer(withGc);
                        var gcCheck = StarterRocket.CheckDesign(catalog, withGc, mission);
                        if (!gcCheck.Blocked) { k = Search(mission, withGc, gcCheck.FairingFits, baseKick); gc = !double.IsNaN(k); }
                    }
                    if (double.IsNaN(k)) { none++; continue; }
                    if (gc) guided++;
                    picked.Add(new Pick { Key = c.Key, Kick = k, GuidanceComputer = gc });
                }
                rows.Add(Row(mission.Id, baseKick, picked));
                Console.WriteLine(mission.Id + ": " + settings + " settings, " + kept + " win at the base kick, " + picked.Count + " picked (" + guided + " with the guidance computer), "
                    + none + " no winning kick (" + sw.Elapsed.TotalSeconds.ToString("0", Inv) + " s)");
            }
            StarterKicks.Disabled = false;

            var sb = new StringBuilder();
            sb.Append("{\n  \"version\": 1,\n");
            sb.Append("  \"note\": \"DECISIONS #85: the stage-1 pitch kick each non-default Starter setting flies (StarterKicks, StarterRocket.Build). Generated by tools/starter-kicks against data/golden/campaign-winners.json in the seed-1 briefing wind; do not hand-edit. A setting with no row keeps the base rocket's kick (it already wins, or no kick wins and the predictor names its cause).\",\n");
            sb.Append("  \"missions\": {\n");
            sb.Append(string.Join(",\n", rows));
            sb.Append("\n  }\n}\n");
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine("wrote " + outPath + " (" + total.Elapsed.TotalMinutes.ToString("0.0", Inv) + " min)");
            return 0;
        }

        /// <summary>probe &lt;mission&gt; &lt;setting key&gt;: the practice flight at every grid kick, one line each (for reading a miss).</summary>
        static int Probe(CampaignMission m, string key)
        {
            StarterKicks.Disabled = true;
            var rocket = new StarterRocket(catalog, m, StarterBases.For(m.Id));
            StarterChoice c = rocket.Default;
            foreach (var s in Settings(rocket)) if (s.Key == key) c = s;
            var json = rocket.BuildDesign(c);
            if (Environment.GetEnvironmentVariable("KH_PROBE_GC") == "1") StarterKicks.FitGuidanceComputer(json);
            var check = StarterRocket.CheckDesign(catalog, json, m);
            Console.WriteLine(m.Id + " " + c.Key + " margin " + check.Margin.ToString("0.000", Inv) + " twr " + check.LiftoffTwr.ToString("0.00", Inv) + " base kick " + StarterKicks.KickOf(json));
            for (double k = KickMin; k <= KickMax + 1e-9; k += 0.5)
            {
                var d = Design.FromJson(WithKick(json, k));
                var sim = ReferenceAscent.Flight(catalog, d).Run();
                var v = StarterVerdict.Judge(catalog, d, m, sim, check.FairingFits);
                Console.WriteLine("  kick " + k.ToString("0.0", Inv) + ": " + sim.outcome + " " + (MissionScore.FlownApoAltM(sim) / 1000).ToString("0.0", Inv) + " x "
                    + (sim.orbit.periAlt / 1000).ToString("0.0", Inv) + " km maxQ " + (sim.maxQ / 1000).ToString("0.0", Inv) + " kPa maxG " + sim.maxG.ToString("0.0", Inv)
                    + (sim.failure != null ? " FAIL " + sim.failure.kind + "/" + sim.failure.cause + " T+" + sim.failure.t.ToString("0", Inv) : "")
                    + " -> " + (v.Passes ? "PASS" : (v.Cause ?? v.Miss)));
            }
            return 0;
        }

        sealed class Pick
        {
            public string Key;
            public double Kick;
            public bool GuidanceComputer;
        }

        static List<Pick> Picks(JsonObject o)
        {
            var list = new List<Pick>();
            if (o == null) return list;
            foreach (var k in o.Order)
            {
                if (o.Obj(k) is JsonObject e) list.Add(new Pick { Key = k, Kick = e.Num("kickDeg"), GuidanceComputer = e.Bool("guidanceComputer") });
                else list.Add(new Pick { Key = k, Kick = o.Num(k) });
            }
            return list;
        }

        static string Row(string id, double baseKick, List<Pick> kicks)
        {
            var sb = new StringBuilder();
            sb.Append("    \"").Append(id).Append("\": { \"baseKickDeg\": ").Append(double.IsNaN(baseKick) ? "null" : Num(baseKick)).Append(", \"kicks\": {");
            for (int i = 0; i < kicks.Count; i++)
                sb.Append(i == 0 ? "\n" : ",\n").Append("      \"").Append(kicks[i].Key).Append("\": ")
                    .Append(kicks[i].GuidanceComputer ? "{ \"kickDeg\": " + Num(kicks[i].Kick) + ", \"guidanceComputer\": true }" : Num(kicks[i].Kick));
            sb.Append(kicks.Count > 0 ? "\n    } }" : "} }");
            return sb.ToString();
        }

        static string Num(double v) => v.ToString("0.###", Inv);

        static IEnumerable<StarterChoice> Settings(StarterRocket r)
        {
            foreach (var e in StarterRocket.EngineOrder)
                foreach (var s in StarterRocket.StageValues)
                    for (int f = 0; f < StarterRocket.FuelNotches; f++)
                        foreach (var b in StarterRocket.BoosterValues)
                            if (r.IsStagesAllowed(s) && r.IsBoostersAllowed(b)) yield return new StarterChoice(e, s, f, b);
        }

        static JsonObject WithKick(JsonObject json, double kick)
        {
            var d = JsonMut.CloneObject(json);
            StarterKicks.Apply(d, kick);
            return d;
        }

        static bool Wins(CampaignMission m, JsonObject json, bool? fits) => Practice(m, json, fits) && Live(m, json, fits);

        static bool Practice(CampaignMission m, JsonObject json, bool? fits)
        {
            var d = Design.FromJson(json);
            var sim = ReferenceAscent.Flight(catalog, d).Run();
            return StarterVerdict.Judge(catalog, d, m, sim, fits).Passes;
        }

        static bool Live(CampaignMission m, JsonObject json, bool? fits)
        {
            var d = Design.FromJson(json);
            return StarterVerdict.Judge(catalog, d, m, LivePad(m, d), fits).Passes;
        }

        static double Search(CampaignMission m, JsonObject json, bool? fits, double baseKick)
        {
            int n = (int)Math.Round((KickMax - KickMin) / KickStep) + 1;
            var pass = new bool[n];
            for (int i = 0; i < n; i++) pass[i] = Practice(m, WithKick(json, KickMin + i * KickStep), fits);
            var runs = new List<(int lo, int hi)>();
            for (int i = 0; i < n; i++)
            {
                if (!pass[i]) continue;
                int j = i;
                while (j + 1 < n && pass[j + 1]) j++;
                runs.Add((i, j));
                i = j;
            }
            double anchor = double.IsNaN(baseKick) ? 0 : baseKick;
            runs.Sort((a, b) =>
            {
                int w = (b.hi - b.lo).CompareTo(a.hi - a.lo);
                if (w != 0) return w;
                double da = Math.Abs(KickMin + 0.5 * (a.lo + a.hi) * KickStep - anchor), db = Math.Abs(KickMin + 0.5 * (b.lo + b.hi) * KickStep - anchor);
                return da.CompareTo(db);
            });
            foreach (var run in runs)
            {
                int mid = (run.lo + run.hi) / 2;
                for (int off = 0; off <= run.hi - run.lo; off++)
                    foreach (int i in off == 0 ? new[] { mid } : new[] { mid + off, mid - off })
                    {
                        if (i < run.lo || i > run.hi) continue;
                        double k = KickMin + i * KickStep;
                        if (Live(m, WithKick(json, k), fits)) return k;
                    }
            }
            return double.NaN;
        }

        /// <summary>The live pad as AscentFlight.Prepare and FlightDriver fly it on default settings: seed 1, the briefing
        /// card's wind and site, auto-assist on, LiveDt physics advanced in 1/60 s frames, played checks.</summary>
        static SimResult LivePad(CampaignMission mission, Design design)
        {
            ulong hash = FlightBatch.DesignHash(design);
            const ulong seed = 1;
            var card = ReferenceAscent.Briefing(design);
            var weather = card?.Wind;
            double target = Stack.RequiredDeltaV(catalog, design.payloadOrbit).targetAlt;
            if (MissionAim.IsApogeeHop(mission)) target = mission.GuidanceAltM;
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            var monitor = FailureMonitor.Engages(design) ? FailureMonitor.Create(catalog, design, target, hash, seed) : null;
            var director = MissionFlightSetup.Director(catalog, design, monitor?.Events, weather, qLim, FailureMonitor.GentleTier(design),
                target, hash, seed, MissionFlightSetup.DefaultAutoAssist, false, false, card?.Bonuses);
            var site = new MissionFlightSite { Wind = weather };
            if (card != null && card.Site != null) { site.HasLatitude = true; site.LatitudeDeg = card.Site.LatitudeDeg; }
            var setup = MissionFlightSetup.Build(catalog, mission, design, MissionFlightSetup.LiveDt, site);
            var opts = setup.Options;
            if (monitor != null) opts.monitor = monitor;
            var sim = new AscentSim(setup.Stages, opts);
            var directed = new DirectedFlight(sim, director, opts);
            var checks = new PlayedChecks(catalog, design, setup.Stages, opts, sim);
            double clock = 0;
            int frames = 0;
            bool stop = false;
            while (!sim.Done && !stop && frames++ < 400000)
            {
                clock += 1.0 / 60.0;
                int guard = 0;
                while (sim.T < clock && !sim.Done && guard++ < 64)
                {
                    int before = sim.Events.Count;
                    directed.Step();
                    checks.NoteEvents(sim, before);
                    if (checks.StopForBreakup(sim)) { stop = true; break; }
                }
            }
            return directed.Finish();
        }
    }
}

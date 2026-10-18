// Deterministic catalog search for a campaign winner.
// Uses only designer ranges and catalog parts. No hidden bonuses.
// Usage: dotnet run -c Release -- [missionId ...]
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;

namespace CampaignSearch
{
    static class Program
    {
        static Catalog Catalog;
        static Campaign Campaign;
        static readonly Dictionary<string, List<Design>> Donors = new Dictionary<string, List<Design>>();
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static int Main(string[] args)
        {
            string root = Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
            Catalog = Catalog.LoadFromRepo(root);
            Campaign = Campaign.Load(root);
            LoadDonors(root);
            if (args != null && args.Length > 0 && args[0] == "report")
            {
                Report(root);
                return 0;
            }
            if (args != null && args.Length > 0 && args[0] == "freeze")
            {
                Freeze(root);
                return 0;
            }
            if (args != null && args.Length > 0 && args[0] == "fixtry")
            {
                FixTry(root);
                return 0;
            }
            var want = new HashSet<string>();
            if (args != null)
                for (int i = 0; i < args.Length; i++)
                    if (!string.IsNullOrEmpty(args[i])) want.Add(args[i]);

            var found = new List<Design>();
            for (int index = 1; index <= 20; index++)
            {
                var mission = Campaign.Mission(index);
                if (want.Count > 0 && !want.Contains(mission.Id) && !want.Contains("KH-" + index)) continue;
                var sw = Stopwatch.StartNew();
                var winner = Search(mission, out string note);
                sw.Stop();
                if (winner == null)
                {
                    Console.WriteLine("KH-" + index + " NONE " + sw.Elapsed.TotalSeconds.ToString("0.0", Inv) + "s " + note);
                    continue;
                }
                found.Add(winner);
                var flight = FlightBatch.Fly(Catalog, mission, winner, Campaign.Seeds[0]);
                Console.WriteLine("KH-" + index + " WIN " + winner.id
                    + " apo " + flight.apoKm.ToString("0.0", Inv)
                    + " peri " + flight.periKm.ToString("0.0", Inv)
                    + " g " + flight.maxG.ToString("0.00", Inv)
                    + " q " + flight.qPeakKPa.ToString("0.0", Inv)
                    + " cost " + flight.costKc.ToString("0", Inv)
                    + " land " + (flight.landingGrade ?? "-") + "/" + (flight.boosterGrade ?? "-")
                    + " " + sw.Elapsed.TotalSeconds.ToString("0.0", Inv) + "s"
                    + " " + note);
            }
            if (found.Count > 0) Write(root, found);
            return 0;
        }

        static void LoadDonors(string root)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "golden", "campaign-viable.json")));
            foreach (JsonObject block in file.Arr("missions"))
            {
                var list = new List<Design>();
                foreach (JsonObject raw in block.Arr("designs"))
                    list.Add(Design.FromJson(raw));
                Donors[block.Str("id")] = list;
            }
        }

        static Design Search(CampaignMission mission, out string note)
        {
            note = "";
            if (Donors.TryGetValue(mission.Id, out var own))
            {
                for (int i = 0; i < own.Count; i++)
                {
                    var design = Stamp(own[i].Clone(), mission, own[i].id);
                    if (Passes(mission, design))
                    {
                        note = "golden";
                        return design;
                    }
                    Console.WriteLine("  golden miss " + own[i].id + " " + Describe(mission, design));
                }
            }

            var seeds = SeedDesigns(mission);
            var seen = new HashSet<string>();
            var queue = new List<Design>();
            for (int i = 0; i < seeds.Count; i++) Enqueue(queue, seen, seeds[i]);

            Design best = null;
            string bestWhy = "";
            double bestScore = double.NegativeInfinity;
            int tried = 0;
            int limit = mission.Index >= 16 ? 160 : 80;
            while (queue.Count > 0 && tried < limit)
            {
                var design = queue[0];
                queue.RemoveAt(0);
                tried++;
                var one = FlightBatch.Fly(Catalog, mission, design, Campaign.Seeds[0]);
                double score = Score(mission, one);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = design;
                    bestWhy = one.failure ?? one.criterion ?? one.outcome ?? "miss";
                }
                if (one.passed && Passes(mission, design))
                {
                    note = "search n=" + tried;
                    design.id = "kh" + mission.Index + "-win";
                    design.name = design.id;
                    return design;
                }
                if (tried <= 6 || score > bestScore - 1)
                    Expand(mission, design, one, queue, seen);
            }
            note = "tried " + tried + " best " + bestWhy + " " + (best == null ? "" : Describe(mission, best));
            return null;
        }

        static void Enqueue(List<Design> queue, HashSet<string> seen, Design design)
        {
            if (design == null || design.stages == null || design.stages.Count == 0) return;
            Legalize(design);
            string key = Finger(design);
            if (!seen.Add(key)) return;
            queue.Add(design);
        }

        static List<Design> SeedDesigns(CampaignMission mission)
        {
            var list = new List<Design>();
            string[] donors = DonorsFor(mission.Id);
            for (int i = 0; i < donors.Length; i++)
            {
                if (!Donors.TryGetValue(donors[i], out var rows)) continue;
                for (int j = 0; j < rows.Count; j++)
                    list.Add(Adapt(rows[j], mission));
            }
            list.Add(Scratch(mission, "gg-kerolox", 7, 280000, "exp-hydrolox", 2, 32000, 3.6));
            list.Add(Scratch(mission, "sc-kerolox", 5, 320000, "exp-hydrolox", 1, 40000, 4.2));
            list.Add(Scratch(mission, "gg-kerolox", 9, 360000, "gg-kerolox-vac", 1, 80000, 3.8));
            if (mission.Index <= 4)
                list.Add(Scratch(mission, "gg-methalox-small", 5, 24000, "exp-hydrolox-small", 1, 2500, 1.6));
            return list;
        }

        static string[] DonorsFor(string id)
        {
            switch (id)
            {
                case "m11": return new[] { "m4", "m5", "m3" };
                case "m12": return new[] { "m4", "m8", "m3" };
                case "m13": return new[] { "m7", "m6", "m4" };
                case "m14": return new[] { "m5", "m8", "m4" };
                case "m15": return new[] { "m7", "m10", "m9" };
                case "m16": return new[] { "m8", "m7", "m5" };
                case "m17": return new[] { "m9", "m7", "m10" };
                case "m18": return new[] { "m9", "m7" };
                case "m19": return new[] { "m10", "m7", "m6" };
                case "m20": return new[] { "m9", "m10" };
                default: return new[] { "m4", "m5", "m9" };
            }
        }

        static Design Adapt(Design donor, CampaignMission mission)
        {
            var d = Stamp(donor.Clone(), mission, "adapt");
            if (!mission.LandingRequired)
            {
                d.legs = false;
                d.gridFins = false;
                d.rcs = false;
                d.recoveryMode = null;
                if (d.stages.Count > 0) d.stages[0].landingPropMass = 0;
                if (d.strapOns != null) d.strapOns.landingPropMass = 0;
            }
            else
            {
                d.legs = true;
                d.gridFins = true;
                d.rcs = true;
                d.recoveryMode = mission.LandingMode;
                if (d.stages.Count > 0 && !(d.stages[0].landingPropMass > 0))
                    d.stages[0].landingPropMass = Math.Floor(d.stages[0].propMass * 0.12);
            }
            if (!mission.LandBoosters)
            {
                d.strapOns = null;
            }
            else if (d.strapOns == null || d.strapOns.count < 2)
            {
                d.strapOns = new StagePart
                {
                    name = "strap",
                    propellant = "kerolox",
                    engine = "gg-kerolox-small",
                    engineCount = 1,
                    count = 2,
                    propMass = 14000,
                    diameter = 1.6,
                    material = "titanium",
                    separationMass = 40,
                    landingPropMass = 2800,
                    clusterPattern = "auto",
                    nozzle = "default",
                };
            }
            else if (!(d.strapOns.landingPropMass > 0))
                d.strapOns.landingPropMass = Math.Floor(d.strapOns.propMass * 0.2);
            if (mission.ReliabilityMin >= 0.98 || mission.NoRedRisks) d.avionicsTier = "redundant";
            if (!double.IsNaN(mission.MaxG)) SetG(d, mission.MaxG);
            if (mission.CoastHours > 0) Insulate(d);
            FitFairing(d, mission);
            return d;
        }

        static Design Scratch(CampaignMission mission, string e1, int n1, double p1, string e2, int n2, double p2, double dia)
        {
            var d = new Design
            {
                version = 3,
                id = "scratch",
                name = "scratch",
                tier = "campaign",
                avionicsTier = mission.ReliabilityMin >= 0.98 ? "redundant" : "standard",
                safetyFactor = 1.8,
                heatShieldSet = true,
                heatShield = true,
                fins = true,
                finCount = 4,
                finCountSet = true,
                finSpanM = Math.Min(6, Math.Max(0.6, dia * 0.35)),
                finSpanSet = true,
                nose = "ogive",
            };
            d.stages.Add(Stage(e1, n1, p1, dia, mission.LandingRequired ? p1 * 0.12 : 0));
            if (mission.Index >= 3) d.stages.Add(Stage(e2, n2, p2, dia, 0));
            if (e2 != null && e2.Contains("hydrolox"))
            {
                d.stages[d.stages.Count - 1].propellant = "hydrolox";
                d.stages[d.stages.Count - 1].tank = new TankParams { insulationSet = true, insulation = "mli" };
            }
            d.guidance = new GuidanceSpec
            {
                pitchKickDegSet = true, pitchKickDeg = 6,
                gLimitSet = true, gLimit = double.IsNaN(mission.MaxG) ? 6 : mission.MaxG,
                maxQThrottleSet = true, maxQThrottle = 50,
            };
            if (mission.LandBoosters)
            {
                d.strapOns = new StagePart
                {
                    name = "strap", propellant = "kerolox", engine = "gg-kerolox-small", engineCount = 1, count = 2,
                    propMass = 20000, diameter = 1.6, material = "titanium", separationMass = 40,
                    landingPropMass = 4000, clusterPattern = "auto", nozzle = "default",
                };
            }
            return Adapt(d, mission);
        }

        static StagePart Stage(string engine, int count, double prop, double dia, double landing)
        {
            string propellant = engine.Contains("hydrolox") ? "hydrolox" : engine.Contains("methalox") ? "methalox" : "kerolox";
            return new StagePart
            {
                name = engine,
                propellant = propellant,
                engine = engine,
                engineCount = count,
                propMass = prop,
                diameter = dia,
                material = "titanium",
                separationMass = 40,
                landingPropMass = landing,
                clusterPattern = "auto",
                nozzle = "default",
                engineParams = new EngineParams { throttleMinSet = true, throttleMin = 0.4 },
            };
        }

        static void Expand(CampaignMission mission, Design design, CampaignFlight flight, List<Design> queue, HashSet<string> seen)
        {
            string why = flight.failure ?? "";
            if (why == "structural-maxq" || flight.qPeakKPa > 45)
            {
                VarySf(design, mission, queue, seen, 2.0, 2.2, 2.4, 2.6, 3.0);
                VaryThrottle(design, mission, queue, seen, 40, 50, 60);
                VaryMaterial(design, mission, queue, seen, "steel", "titanium");
                VaryPitch(design, mission, queue, seen, 4, 6, 8);
            }
            if (why == "overg" || (!double.IsNaN(mission.MaxG) && flight.maxG > mission.MaxG))
            {
                SetG(design, double.IsNaN(mission.MaxG) ? 4.5 : mission.MaxG);
                Enqueue(queue, seen, design.Clone());
                VaryPitch(design, mission, queue, seen, 3, 4, 5);
                VaryThrottle(design, mission, queue, seen, 40, 55);
                VaryEngines(design, mission, queue, seen, 0, 1, 2);
            }
            if (why == "engine-out")
            {
                var clone = design.Clone();
                clone.avionicsTier = "redundant";
                if (clone.stages.Count > 0 && clone.stages[0].engineCount < 9)
                    clone.stages[0].engineCount = 9;
                Enqueue(queue, seen, clone);
                VaryEngines(design, mission, queue, seen, 2, 4);
            }
            if (why == "prop-depletion" || why == "" || flight.apoKm < mission.ApoKm * 0.9 || (mission.TargetKind != "apogee" && flight.periKm < mission.PeriKm * 0.9))
            {
                VaryUpper(design, mission, queue, seen, 1.15, 1.3, 1.5, 1.8);
                VaryCore(design, mission, queue, seen, 1.1, 1.25, 1.4);
                VaryEngines(design, mission, queue, seen, 1, 2, -1);
            }
            if (mission.LandingRequired && (flight.landingGrade == "short" || flight.landingGrade == "hard" || flight.landingGrade == null))
            {
                VaryReserve(design, mission, queue, seen, 0.15, 0.18, 0.2);
                VaryCore(design, mission, queue, seen, 0.85, 0.7);
            }
            if (mission.LandBoosters && (flight.boosterGrade == "short" || flight.boosterGrade == "hard"))
            {
                VaryStrapReserve(design, mission, queue, seen, 0.15, 0.2);
            }
            if (!flight.underBudget)
            {
                VaryMaterial(design, mission, queue, seen, "steel");
                VaryEngines(design, mission, queue, seen, -1, -2);
            }
        }

        static void VarySf(Design d, CampaignMission m, List<Design> q, HashSet<string> seen, params double[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                var c = d.Clone();
                c.safetyFactor = values[i];
                Enqueue(q, seen, c);
            }
        }

        static void VaryThrottle(Design d, CampaignMission m, List<Design> q, HashSet<string> seen, params double[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                var c = d.Clone();
                c.guidance = c.guidance?.Clone() ?? new GuidanceSpec();
                c.guidance.maxQThrottle = values[i];
                c.guidance.maxQThrottleSet = true;
                Enqueue(q, seen, c);
            }
        }

        static void VaryPitch(Design d, CampaignMission m, List<Design> q, HashSet<string> seen, params double[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                var c = d.Clone();
                c.guidance = c.guidance?.Clone() ?? new GuidanceSpec();
                c.guidance.pitchKickDeg = values[i];
                c.guidance.pitchKickDegSet = true;
                Enqueue(q, seen, c);
            }
        }

        static void VaryMaterial(Design d, CampaignMission m, List<Design> q, HashSet<string> seen, params string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                var c = d.Clone();
                for (int s = 0; s < c.stages.Count; s++)
                {
                    if (c.stages[s].propellant == "hydrolox" && values[i] == "composite") continue;
                    c.stages[s].material = values[i];
                }
                if (c.fairingMaterial != null) c.fairingMaterial = values[i] == "composite" ? c.fairingMaterial : values[i];
                Enqueue(q, seen, c);
            }
        }

        static void VaryUpper(Design d, CampaignMission m, List<Design> q, HashSet<string> seen, params double[] scales)
        {
            if (d.stages.Count < 2) return;
            for (int i = 0; i < scales.Length; i++)
            {
                var c = d.Clone();
                int last = c.stages.Count - 1;
                c.stages[last].propMass = Math.Round(c.stages[last].propMass * scales[i]);
                Enqueue(q, seen, c);
            }
        }

        static void VaryCore(Design d, CampaignMission m, List<Design> q, HashSet<string> seen, params double[] scales)
        {
            for (int i = 0; i < scales.Length; i++)
            {
                var c = d.Clone();
                c.stages[0].propMass = Math.Round(c.stages[0].propMass * scales[i]);
                if (c.stages[0].landingPropMass > 0)
                    c.stages[0].landingPropMass = Math.Min(c.stages[0].landingPropMass, Math.Floor(c.stages[0].propMass * 0.2));
                Enqueue(q, seen, c);
            }
        }

        static void VaryEngines(Design d, CampaignMission m, List<Design> q, HashSet<string> seen, params int[] delta)
        {
            for (int i = 0; i < delta.Length; i++)
            {
                var c = d.Clone();
                int n = c.stages[0].engineCount + delta[i];
                if (n < 1 || n > 20) continue;
                c.stages[0].engineCount = n;
                c.stages[0].clusterPattern = "auto";
                Enqueue(q, seen, c);
            }
        }

        static void VaryReserve(Design d, CampaignMission m, List<Design> q, HashSet<string> seen, params double[] frac)
        {
            for (int i = 0; i < frac.Length; i++)
            {
                var c = d.Clone();
                c.legs = true;
                c.gridFins = true;
                c.rcs = true;
                c.recoveryMode = m.LandingMode;
                c.stages[0].landingPropMass = Math.Floor(c.stages[0].propMass * frac[i]);
                Enqueue(q, seen, c);
            }
        }

        static void VaryStrapReserve(Design d, CampaignMission m, List<Design> q, HashSet<string> seen, params double[] frac)
        {
            if (d.strapOns == null) return;
            for (int i = 0; i < frac.Length; i++)
            {
                var c = d.Clone();
                c.strapOns.landingPropMass = Math.Floor(c.strapOns.propMass * frac[i]);
                Enqueue(q, seen, c);
            }
        }

        static Design Stamp(Design d, CampaignMission mission, string id)
        {
            d.id = id;
            d.name = id;
            d.tier = "campaign";
            d.missionId = mission.Id;
            d.payloadType = mission.PayloadId;
            d.payloadMass = mission.MassKg;
            d.payloadOrbit = string.IsNullOrEmpty(mission.OrbitId) ? "kh-" + mission.Id : mission.OrbitId;
            d.version = 3;
            FitFairing(d, mission);
            return d;
        }

        static void FitFairing(Design d, CampaignMission mission)
        {
            double dia = mission.DiameterM + 0.2;
            if (d.fairingDiameter > dia) dia = d.fairingDiameter;
            dia = Math.Ceiling(dia * 10 - 1e-9) / 10.0;
            if (dia < 0.6) dia = 0.6;
            double radius = 0.45 * dia;
            double denom = Math.PI * radius * radius * 0.8;
            double length = denom > 0 ? mission.VolumeM3 / denom * 1.05 : 3;
            if (d.fairingLength > length) length = d.fairingLength;
            length = Math.Ceiling(length * 10 - 1e-9) / 10.0;
            if (length < 2) length = 2;
            d.fairingDiameter = dia;
            d.fairingLength = length;
            if (string.IsNullOrEmpty(d.fairingMaterial)) d.fairingMaterial = "titanium";
            d.fairingJettisonAltitudeKm = 110;
        }

        static void SetG(Design d, double g)
        {
            d.guidance = d.guidance ?? new GuidanceSpec();
            d.guidance.gLimit = g;
            d.guidance.gLimitSet = true;
        }

        static void Insulate(Design d)
        {
            for (int i = 0; i < d.stages.Count; i++)
            {
                var s = d.stages[i];
                if (s.propellant != "hydrolox" && s.propellant != "methalox") continue;
                s.tank = s.tank ?? new TankParams();
                s.tank.insulationSet = true;
                s.tank.insulation = "mli";
            }
        }

        static void Legalize(Design d)
        {
            d.safetyFactor = Snap(d.safetyFactor, 0.05, 1.0, 3.0);
            for (int i = 0; i < d.stages.Count; i++) LegalStage(d.stages[i], i == 0);
            if (d.strapOns != null && d.strapOns.count > 0)
            {
                LegalStage(d.strapOns, true);
                if (d.strapOns.count < 2) d.strapOns.count = 2;
            }
            else d.strapOns = null;
            if (d.guidance != null && d.guidance.maxQThrottleSet)
            {
                double pct = d.guidance.maxQThrottle > 1.5 ? d.guidance.maxQThrottle : d.guidance.maxQThrottle * 100;
                pct = Snap(pct, 5, 40, 100);
                d.guidance.maxQThrottle = pct;
            }
            if (d.finSpanSet) d.finSpanM = Snap(d.finSpanM, 0.1, 0.2, 6);
        }

        static void LegalStage(StagePart s, bool booster)
        {
            if (s.engineCount < 1) s.engineCount = 1;
            if (s.engineCount > 40) s.engineCount = 40;
            s.propMass = Math.Max(1, Math.Round(s.propMass));
            s.diameter = Snap(s.diameter, 0.1, 0.3, 15);
            if (s.landingPropMass < 0) s.landingPropMass = 0;
            double cap = Math.Floor(s.propMass * 0.2);
            if (s.landingPropMass > cap) s.landingPropMass = cap;
            if (booster && Catalog.EngineById.TryGetValue(s.engine ?? "", out var pad) && pad.thrustSL <= 0)
            {
                s.engine = "gg-kerolox";
                s.propellant = "kerolox";
            }
            if (Catalog.EngineById.TryGetValue(s.engine ?? "", out var e))
            {
                if (s.propellant != e.propellant) s.propellant = e.propellant;
                double bell = e.diameter > 0 ? e.diameter : 1;
                double need = Math.Sqrt(s.engineCount * bell * bell / 1.1) + 0.05;
                if (s.diameter < need) s.diameter = Snap(need, 0.1, 0.3, 15);
            }
            if (s.propellant == "hydrolox" && s.material == "composite") s.material = "titanium";
            if (string.IsNullOrEmpty(s.clusterPattern)) s.clusterPattern = "auto";
        }

        static double Snap(double value, double step, double min, double max)
        {
            double n = Math.Round(value / step) * step;
            if (n < min) n = min;
            if (n > max) n = max;
            return Math.Round(n, 6);
        }

        static bool Passes(CampaignMission mission, Design design)
        {
            for (int i = 0; i < Campaign.Seeds.Count; i++)
            {
                var flight = FlightBatch.Fly(Catalog, mission, design, Campaign.Seeds[i]);
                if (!flight.passed) return false;
            }
            LegalReport(design);
            return true;
        }

        static bool LegalReport(Design design)
        {
            try
            {
                var report = Validate.Engineering(Catalog, design.ToJson());
                if (report != null && report.blocks != null && report.blocks.Count > 0)
                {
                    Console.WriteLine("  blocks " + design.id + " " + report.blocks[0].code + " " + report.blocks[0].msg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  validate " + ex.Message);
                return false;
            }
            return true;
        }

        static double Score(CampaignMission mission, CampaignFlight f)
        {
            if (f.passed) return 1e9;
            double s = 0;
            if (f.failure == null) s += 1000;
            if (mission.TargetKind == "apogee") s += Math.Min(f.apoKm / Math.Max(mission.ApoKm, 1), 1.2) * 100;
            else
            {
                s += Math.Min(f.periKm / Math.Max(mission.PeriKm, 1), 1.2) * 80;
                s += Math.Min(f.idealDv / Math.Max(f.requiredDv, 1), 1.2) * 40;
            }
            if (f.failure == "structural-maxq") s -= 30;
            if (f.failure == "engine-out") s -= 80;
            if (!f.underBudget) s -= 50;
            if (!f.fairingFits) s -= 40;
            if (mission.LandingRequired && f.landingGrade == "good") s += 40;
            if (mission.LandingRequired && f.landingGrade == "perfect") s += 50;
            if (mission.LandBoosters && f.boosterGrade == "good") s += 40;
            return s;
        }

        static string Describe(CampaignMission mission, Design design)
        {
            var f = FlightBatch.Fly(Catalog, mission, design, Campaign.Seeds[0]);
            return (f.passed ? "PASS" : (f.failure ?? "short"))
                + " intact=" + (f.outcome ?? "")
                + " hit=" + (f.criterion ?? "-")
                + " pay=" + f.payloadMatch
                + " fair=" + f.fairingFits
                + " bud=" + f.underBudget
                + " gen=" + f.gentle
                + " red=" + f.redRisk
                + " rel=" + f.reliability.ToString("0.000", Inv)
                + " apo " + f.apoKm.ToString("0.0", Inv)
                + " peri " + f.periKm.ToString("0.0", Inv)
                + " g " + f.maxG.ToString("0.00", Inv)
                + " q " + f.qPeakKPa.ToString("0.0", Inv)
                + " land " + (f.landingGrade ?? "-") + "/" + (f.boosterGrade ?? "-")
                + " dv " + f.idealDv.ToString("0", Inv) + "/" + f.requiredDv.ToString("0", Inv)
                + " cost " + f.costKc.ToString("0", Inv);
        }

        static string Finger(Design d)
        {
            var parts = new StringBuilder();
            parts.Append(d.safetyFactor.ToString("0.00", Inv)).Append('|').Append(d.avionicsTier).Append('|');
            if (d.guidance != null && d.guidance.maxQThrottleSet) parts.Append(d.guidance.maxQThrottle.ToString("0", Inv));
            parts.Append('|');
            if (d.guidance != null && d.guidance.gLimitSet) parts.Append(d.guidance.gLimit.ToString("0.0", Inv));
            parts.Append('|');
            if (d.guidance != null && d.guidance.pitchKickDegSet) parts.Append(d.guidance.pitchKickDeg.ToString("0", Inv));
            for (int i = 0; i < d.stages.Count; i++)
            {
                var s = d.stages[i];
                parts.Append('|').Append(s.engine).Append(':').Append(s.engineCount).Append(':')
                    .Append(s.propMass.ToString("0", Inv)).Append(':').Append(s.material).Append(':')
                    .Append(s.landingPropMass.ToString("0", Inv));
            }
            if (d.strapOns != null)
                parts.Append("|s").Append(d.strapOns.count).Append(':').Append(d.strapOns.propMass.ToString("0", Inv))
                    .Append(':').Append(d.strapOns.landingPropMass.ToString("0", Inv));
            return parts.ToString();
        }

        static void Boost(Dictionary<string, Design> byId, string id, int engines)
        {
            if (!byId.TryGetValue(id, out var design) || design.stages == null || design.stages.Count == 0) return;
            design.stages[0].engineCount = engines;
            design.stages[0].clusterPattern = "auto";
            if (design.strapOns != null && design.strapOns.count == 0) design.strapOns = null;
            Legalize(design);
        }

        static Design BuildM13()
        {
            var hold = Adapt(Donors["m4"][1], Campaign.Mission(13));
            hold.stages[0].engineCount = 6;
            hold.stages[0].propMass = 220000;
            hold.stages[0].clusterPattern = "auto";
            hold.legs = true;
            hold.gridFins = true;
            hold.rcs = true;
            hold.recoveryMode = "barge";
            hold.stages[0].landingPropMass = Math.Floor(220000 * 0.04);
            hold.stages[hold.stages.Count - 1].propMass = 32000;
            hold.guidance = new GuidanceSpec { pitchKickDegSet = true, pitchKickDeg = 3, gLimitSet = true, gLimit = 5.5 };
            hold.finSpanM = 3.2;
            hold.finSpanSet = true;
            hold.fins = true;
            hold.id = "kh13-win";
            hold.name = hold.id;
            Legalize(hold);
            return hold;
        }

        static Design BuildM16()
        {
            var hold = Adapt(Donors["m8"][2], Campaign.Mission(16));
            hold.stages[0].engineCount = 9;
            hold.stages[0].clusterPattern = "auto";
            hold.stages[hold.stages.Count - 1].propMass = 35000;
            hold.legs = false;
            hold.gridFins = false;
            hold.rcs = false;
            hold.recoveryMode = null;
            hold.stages[0].landingPropMass = 0;
            hold.guidance = new GuidanceSpec { pitchKickDegSet = true, pitchKickDeg = 6, gLimitSet = true, gLimit = 4.2 };
            hold.avionicsTier = "redundant";
            hold.id = "kh16-win";
            hold.name = hold.id;
            Legalize(hold);
            return hold;
        }

        static void Freeze(string root)
        {
            var byId = new Dictionary<string, Design>();
            string foundPath = Path.Combine(root, "tools", "campaign-search", "found.json");
            if (File.Exists(foundPath))
            {
                var file = MiniJson.ParseObject(File.ReadAllText(foundPath));
                foreach (JsonObject block in file.Arr("missions"))
                    byId[block.Str("id")] = Design.FromJson((JsonObject)block.Arr("designs")[0]);
            }
            byId["m13"] = BuildM13();
            byId["m16"] = BuildM16();
            if (byId.TryGetValue("m18", out var m18) && m18.stages.Count >= 3)
            {
                // The 260 t core draws an engine-out on seed 2. 250 t keeps the same
                // stages and clears C3 -0.4 on every campaign seed.
                m18.stages[0].propMass = 250000;
                m18.stages[1].propMass = 100000;
                m18.stages[2].propMass = 28000;
                m18.id = "kh18-win";
                m18.name = m18.id;
            }
            // Four kerolox engines under this stack sit under 1.0 liftoff TWR, which the designer blocks.
            // Six (seven on KH-20) clear 1.0 and still make the target on every seed.
            Boost(byId, "m9", 6);
            Boost(byId, "m17", 6);
            Boost(byId, "m18", 6);
            Boost(byId, "m20", 7);
            var found = new List<Design>();
            bool all = true;
            for (int index = 1; index <= 20; index++)
            {
                string id = "m" + index;
                var mission = Campaign.Mission(index);
                if (!byId.TryGetValue(id, out var design))
                {
                    Console.WriteLine("KH-" + index + " MISSING");
                    all = false;
                    continue;
                }
                design.missionId = id;
                design.tier = "campaign";
                design = Design.FromJson(LegalDoc(design.ToJson()));
                if (!Passes(mission, design))
                {
                    Console.WriteLine("KH-" + index + " FAIL " + Describe(mission, design));
                    for (int s = 0; s < Campaign.Seeds.Count; s++)
                    {
                        var miss = FlightBatch.Fly(Catalog, mission, design, Campaign.Seeds[s]);
                        if (miss.passed) continue;
                        Console.WriteLine("  seed " + Campaign.Seeds[s] + " " + (miss.failure ?? "short")
                            + " apo " + miss.apoKm.ToString("0.00", Inv)
                            + " peri " + miss.periKm.ToString("0.00", Inv)
                            + " c3 " + miss.c3Km2PerS2.ToString("0.000", Inv)
                            + " rel " + miss.reliability.ToString("0.000", Inv)
                            + " red " + miss.redRisk);
                    }
                    all = false;
                    continue;
                }
                var flight = FlightBatch.Fly(Catalog, mission, design, Campaign.Seeds[0]);
                Console.WriteLine("KH-" + index + " WIN " + design.id
                    + " apo " + flight.apoKm.ToString("0.0", Inv)
                    + " peri " + flight.periKm.ToString("0.0", Inv)
                    + " g " + flight.maxG.ToString("0.00", Inv)
                    + " q " + flight.qPeakKPa.ToString("0.0", Inv)
                    + " land " + (flight.landingGrade ?? "-") + "/" + (flight.boosterGrade ?? "-"));
                found.Add(design);
            }
            if (!all || found.Count != 20)
            {
                Console.WriteLine("freeze incomplete");
                return;
            }
            WriteWinners(root, found);
        }

        static void FixTry(string root)
        {
            string path = Path.Combine(root, "data", "golden", "campaign-winners.json");
            var file = MiniJson.ParseObject(File.ReadAllText(path));
            var winners = new Dictionary<string, Design>();
            foreach (JsonObject block in file.Arr("missions"))
                winners[block.Str("id")] = Design.FromJson((JsonObject)block.Arr("designs")[0]);

            // fix: lower strength, Auto Fix it, every seed passes.
            // strength: Fix it also widens the fairing or adds engines and misses. Restoring strength is the edit.
            // fly: the winner is already the board rocket. Fix it would change engines and can draw an engine-out.
            var plan = new[]
            {
                Row("m3", "strength", 1.45),
                Row("m4", "fix", 1.25),
                Row("m5", "fix", 1.25),
                Row("m6", "strength", 1.35),
                Row("m7", "fly", 0),
                Row("m8", "strength", 1.20),
                Row("m9", "fly", 0),
                Row("m10", "fly", 0),
                Row("m11", "fix", 1.55),
                Row("m12", "strength", 1.55),
                Row("m13", "fix", 1.25),
                Row("m14", "strength", 1.35),
                Row("m15", "fly", 0),
                Row("m16", "strength", 1.35),
                Row("m17", "fly", 0),
                Row("m18", "fly", 0),
                Row("m19", "fly", 0),
                Row("m20", "fly", 0),
            };
            var starters = new List<object>();
            bool ok = true;
            for (int i = 0; i < plan.Length; i++)
            {
                var row = plan[i];
                var mission = Campaign.ById(row.Id);
                var winner = winners[row.Id];
                var starter = winner.Clone();
                if (row.Sf > 0) starter.safetyFactor = row.Sf;
                bool raw = FlightBatch.Fly(Catalog, mission, starter, Campaign.Seeds[0]).passed;
                string edit;
                if (row.Mode == "fix")
                {
                    bool fixedOk = TryFix(mission, starter, out _, out string note);
                    edit = "Auto Fix it. " + note;
                    if (!fixedOk || raw)
                    {
                        ok = false;
                        Console.WriteLine(row.Id + " FIX BAD raw " + raw + " " + note);
                    }
                    else Console.WriteLine(row.Id + " fix raw-fail " + note);
                }
                else if (row.Mode == "strength")
                {
                    var back = starter.Clone();
                    back.safetyFactor = winner.safetyFactor;
                    bool backOk = Passes(mission, back);
                    edit = "Set strength to " + winner.safetyFactor.ToString("0.##", Inv)
                        + ". Fix it also changes the fairing or the engines, and that misses.";
                    if (raw || !backOk)
                    {
                        ok = false;
                        Console.WriteLine(row.Id + " STRENGTH BAD raw " + raw + " restore " + backOk);
                    }
                    else Console.WriteLine(row.Id + " strength " + starter.safetyFactor.ToString("0.00", Inv) + " -> " + winner.safetyFactor.ToString("0.00", Inv));
                }
                else
                {
                    bool fly = Passes(mission, starter);
                    edit = "Fly this rocket. It already clears every seed.";
                    if (!fly)
                    {
                        ok = false;
                        Console.WriteLine(row.Id + " FLY BAD");
                    }
                    else Console.WriteLine(row.Id + " fly");
                }
                var blockOut = new JsonObject();
                blockOut.Add("id", row.Id);
                blockOut.Add("mode", row.Mode);
                blockOut.Add("restoreSafetyFactor", winner.safetyFactor);
                blockOut.Add("edit", edit);
                var designs = new List<object> { LegalDoc(starter.ToJson()) };
                blockOut.Add("designs", designs);
                starters.Add(blockOut);
            }
            if (!ok)
            {
                Console.WriteLine("starters incomplete");
                return;
            }
            var rootObj = new JsonObject();
            rootObj.Add("note", "Board starters for KH-3 through KH-20. KH-1 and KH-2 stay on the armed hopper. mode fix: Auto Fix it wins. mode strength: set safety factor back to restoreSafetyFactor. mode fly: the starter already wins; leave the engines alone.");
            rootObj.Add("missions", starters);
            string outPath = Path.Combine(root, "data", "board-starters.json");
            File.WriteAllText(outPath, Pretty(JsonWriter.Stringify(rootObj)));
            Console.WriteLine("wrote " + outPath);
        }

        struct StarterRow
        {
            public string Id;
            public string Mode;
            public double Sf;
        }

        static StarterRow Row(string id, string mode, double sf) => new StarterRow { Id = id, Mode = mode, Sf = sf };

        static bool TryFix(CampaignMission mission, Design starter, out Design fixedDesign, out string note)
        {
            fixedDesign = null;
            note = "";
            var json = LegalDoc(starter.ToJson());
            try { ReferenceAscent.Run(Catalog, json); }
            catch (Exception ex) { note = "reference " + ex.Message; return false; }
            StepperReport report;
            try { report = StepperModel.Evaluate(Catalog, json); }
            catch (Exception ex) { note = "evaluate " + ex.Message; return false; }
            StepCheck check = null;
            if (report != null && report.Checks != null)
            {
                for (int i = 0; i < report.Checks.Length; i++)
                {
                    if (report.Checks[i] != null && !string.IsNullOrEmpty(report.Checks[i].Code))
                    {
                        check = report.Checks[i];
                        break;
                    }
                }
            }
            if (check == null) { note = "no warning"; return false; }
            StepperFixResult result;
            try { result = StepperFix.Apply(Catalog, json, check); }
            catch (Exception ex) { note = "apply " + ex.Message; return false; }
            note = (result != null && result.Cleared ? "cleared " : "open ") + (result != null ? result.Instruction : "");
            fixedDesign = Design.FromJson(json);
            for (int i = 0; i < Campaign.Seeds.Count; i++)
            {
                var flight = FlightBatch.Fly(Catalog, mission, fixedDesign, Campaign.Seeds[i]);
                if (!flight.passed)
                {
                    note += " seed " + Campaign.Seeds[i] + " " + (flight.failure ?? "short")
                        + " apo " + flight.apoKm.ToString("0.0", Inv);
                    return false;
                }
            }
            return true;
        }

        static JsonObject LegalDoc(JsonObject design)
        {
            if (design == null) return design;
            var so = design.Obj("strapOns");
            if (so == null)
            {
                so = new JsonObject();
                so.Add("count", 0d);
                design.Add("strapOns", so);
            }
            else if (!so.ContainsKey("count"))
                so.Add("count", 0d);
            if (so.Num("count") > 0)
            {
                if (!(so.ContainsKey("kind") && so["kind"] is string))
                    JsonMut.Set(so, "kind", "liquid");
                if (!(so.ContainsKey("crossfeed") && so["crossfeed"] is bool))
                    JsonMut.Set(so, "crossfeed", false);
            }
            return design;
        }

        static void WriteWinners(string root, List<Design> found)
        {
            var missions = new List<object>();
            for (int i = 0; i < found.Count; i++)
            {
                var block = new JsonObject();
                block.Add("id", found[i].missionId);
                var designs = new List<object> { LegalDoc(found[i].ToJson()) };
                block.Add("designs", designs);
                missions.Add(block);
            }
            var rootObj = new JsonObject();
            rootObj.Add("note", "One catalog-legal winner per mission. Gentle physics off. Seeds 1 through 5. KH-16 is the retuned 500 km crew circle.");
            rootObj.Add("missions", missions);
            string path = Path.Combine(root, "data", "golden", "campaign-winners.json");
            File.WriteAllText(path, Pretty(JsonWriter.Stringify(rootObj)));
            Console.WriteLine("wrote " + path);
        }

        static string Pretty(string compact)
        {
            var sb = new StringBuilder();
            int n = 0;
            bool str = false;
            bool esc = false;
            for (int i = 0; i < compact.Length; i++)
            {
                char c = compact[i];
                if (str)
                {
                    sb.Append(c);
                    if (esc) esc = false;
                    else if (c == '\\') esc = true;
                    else if (c == '"') str = false;
                    continue;
                }
                if (c == '"') { str = true; sb.Append(c); continue; }
                if (c == '{' || c == '[')
                {
                    sb.Append(c);
                    n++;
                    sb.Append('\n').Append(' ', n * 2);
                    continue;
                }
                if (c == '}' || c == ']')
                {
                    n--;
                    if (n < 0) n = 0;
                    sb.Append('\n').Append(' ', n * 2).Append(c);
                    continue;
                }
                if (c == ',')
                {
                    sb.Append(c);
                    sb.Append('\n').Append(' ', n * 2);
                    continue;
                }
                if (c == ':') { sb.Append(": "); continue; }
                sb.Append(c);
            }
            sb.Append('\n');
            return sb.ToString();
        }

        static void Report(string root)
        {
            var starters = MiniJson.ParseObject(File.ReadAllText(Path.Combine(root, "data", "board-starters.json")));
            var byMode = new Dictionary<string, JsonObject>();
            foreach (JsonObject row in starters.Arr("missions")) byMode[row.Str("id")] = row;
            for (int index = 1; index <= 20; index++)
            {
                var mission = Campaign.Mission(index);
                string mode = index <= 2 ? "board" : byMode["m" + index].Str("mode");
                Design design;
                if (index <= 2)
                {
                    var choice = HomeAutoBuild.Choose(Catalog, HangarData.LoadFromRepo(root).templates, mission);
                    design = Design.FromJson(choice.Design);
                }
                else
                {
                    var row = byMode[mission.Id];
                    design = Design.FromJson((JsonObject)row.Arr("designs")[0]);
                    if (mode == "strength") design.safetyFactor = row.Num("restoreSafetyFactor");
                    else if (mode == "fix")
                    {
                        var json = LegalDoc(design.ToJson());
                        ReferenceAscent.Run(Catalog, json);
                        var report = StepperModel.Evaluate(Catalog, json);
                        StepCheck check = null;
                        for (int i = 0; i < report.Checks.Length; i++)
                            if (report.Checks[i] != null && !string.IsNullOrEmpty(report.Checks[i].Code)) { check = report.Checks[i]; break; }
                        StepperFix.Apply(Catalog, json, check);
                        design = Design.FromJson(json);
                    }
                }
                var f = FlightBatch.Fly(Catalog, mission, design, Campaign.Seeds[0]);
                Console.WriteLine("KH-" + index + " " + mode
                    + " " + (f.passed ? "pass" : "FAIL")
                    + " apo " + f.apoKm.ToString("0.0", Inv)
                    + " peri " + f.periKm.ToString("0.0", Inv)
                    + " q " + f.qPeakKPa.ToString("0.0", Inv)
                    + " g " + f.maxG.ToString("0.00", Inv)
                    + " " + (f.failure ?? f.criterion ?? ""));
            }
        }

        static void Write(string root, List<Design> found)
        {
            var missions = new List<object>();
            for (int i = 0; i < found.Count; i++)
            {
                var block = new JsonObject();
                block.Add("id", found[i].missionId);
                var designs = new List<object> { found[i].ToJson() };
                block.Add("designs", designs);
                missions.Add(block);
            }
            var rootObj = new JsonObject();
            rootObj.Add("note", "One catalog-legal winner per mission. Gentle physics off. Seeds 1 through 5.");
            rootObj.Add("missions", missions);
            string path = Path.Combine(root, "tools", "campaign-search", "found.json");
            File.WriteAllText(path, JsonWriter.Stringify(rootObj));
            Console.WriteLine("wrote " + path);
        }
    }
}

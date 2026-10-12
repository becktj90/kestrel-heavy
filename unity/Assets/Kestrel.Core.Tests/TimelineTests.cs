using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Timeline;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class TimelineTests
    {
        static readonly string[] SampleIds = { "tpl-sounding-solid", "tpl-medium-kerolox-reusable", "tpl-hydrogen-upper", "super-heavy" };

        [Test]
        public void Clock_EngineerIsReal_KidCountFitsTheBand()
        {
            Assert.AreEqual(-2280, TimelineClock.GameSeconds(-2280, "engineer"), 1e-9);
            Assert.AreEqual(-2100, TimelineClock.GameSeconds(-2100, "engineer"), 1e-9);
            Assert.AreEqual(-240, TimelineClock.GameSeconds(-240, "engineer"), 1e-9);
            Assert.AreEqual(0, TimelineClock.GameSeconds(0, "kid"), 1e-9);
            double prev = double.NegativeInfinity;
            foreach (var real in new[] { -7200d, -4000, -2280, -2100, -960, -420, -240, -45, -3, 0 })
            {
                double kid = TimelineClock.GameSeconds(real, "kid");
                Assert.GreaterOrEqual(kid, -40, "kid " + real);
                Assert.LessOrEqual(kid, 0.0001, "kid " + real);
                Assert.GreaterOrEqual(kid, prev);
                prev = kid;
            }
        }

        [Test]
        public void EveryTemplateAndPreset_IsDeterministic_Ordered_AndCausal()
        {
            var cat = Golden.Catalog;
            foreach (var d in AllVehicles())
            {
                var pad = Pad.For(cat, d);
                var a = TimelineGenerator.Generate(cat, d, pad, new Mission { mode = "standard" }, 1);
                var b = TimelineGenerator.Generate(cat, d, pad, new Mission { mode = "standard" }, 1);
                Assert.AreEqual(TimelineJson.Write(a), TimelineJson.Write(b), d.id + " determinism");
                Assert.IsTrue(a.checks.ok, d.id + ": " + string.Join(" | ", a.checks.errors));
                AssertMonotonic(a, d.id);
                Assert.IsNotEmpty(a.events, d.id);
                foreach (var e in a.events)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(e.strings.explainWhy), e.id);
                    Assert.LessOrEqual(e.strings.shortLabel.Length, 12, e.id);
                }
                foreach (var abs in a.absent) Assert.IsFalse(string.IsNullOrEmpty(abs.explainWhy), abs.id);
            }
        }

        [Test]
        public void ModeChangesTimes_NotIds_SeedChangesVarianceOnly()
        {
            var cat = Golden.Catalog;
            var d = cat.Preset("super-heavy");
            var pad = Pad.For(cat, d);
            var kid = TimelineGenerator.Generate(cat, d, pad, new Mission { mode = "kid" }, 1);
            var eng = TimelineGenerator.Generate(cat, d, pad, new Mission { mode = "engineer" }, 7);
            var again = TimelineGenerator.Generate(cat, d, pad, new Mission { mode = "engineer" }, 7);
            Assert.AreEqual(Ids(eng), Ids(kid));
            Assert.AreEqual(TimelineJson.Write(eng), TimelineJson.Write(again));
            bool timeMoved = false, varianceMoved = false;
            for (int i = 0; i < eng.events.Count; i++)
            {
                if (Math.Abs(eng.events[i].nominal - kid.events[i].nominal) > 1e-6) timeMoved = true;
                if (Math.Abs(eng.events[i].variance - kid.events[i].variance) > 1e-9) varianceMoved = true;
            }
            Assert.IsTrue(timeMoved);
            Assert.IsTrue(varianceMoved);
            var other = TimelineGenerator.Generate(cat, d, pad, new Mission { mode = "engineer" }, 99);
            bool seedVariance = false;
            for (int i = 0; i < eng.events.Count; i++)
            {
                Assert.AreEqual(eng.events[i].nominal, other.events[i].nominal, 1e-9, eng.events[i].id);
                if (Math.Abs(eng.events[i].variance - other.events[i].variance) > 1e-9) seedVariance = true;
            }
            Assert.IsTrue(seedVariance);
        }

        [Test]
        public void HardwareAndRestartRules()
        {
            var cat = Golden.Catalog;
            var solid = LoadTemplate("sounding-solid.json");
            var solidTl = Gen(cat, solid);
            Assert.IsNull(Find(solidTl, "pad.strongback"));
            Assert.IsNull(Find(solidTl, "count.lox.s1.load"));
            Assert.IsNull(Find(solidTl, "asc.meco"));
            Assert.IsNull(Find(solidTl, "ign.stagger"));
            Assert.IsNotNull(Find(solidTl, "count.solid.s1.arm"));
            Assert.IsNotNull(Absence(solidTl, "pad.strongback"));
            Assert.IsNotNull(Absence(solidTl, "asc.meco"));

            var hydro = LoadTemplate("hydrogen-upper.json");
            var hydroTl = Gen(cat, hydro);
            Assert.IsNotNull(Find(hydroTl, "count.lh2.s2.leak"));
            Assert.IsNotNull(Find(hydroTl, "stg.ses.s2.1"));
            Assert.IsNotNull(Find(hydroTl, "stg.ses.s2.2"), "GTO hydrolox upper is restartable and needs a transfer burn");
            Assert.IsNotNull(Find(hydroTl, "stg.fairing"));
            Assert.AreEqual("transfer", Find(hydroTl, "stg.ses.s2.2").phase);

            var reuse = LoadTemplate("medium-kerolox-reusable.json");
            var reuseTl = Gen(cat, reuse);
            Assert.AreEqual("droneship", reuseTl.recovery);
            Assert.IsNotNull(Find(reuseTl, "rec.entry"));
            Assert.IsNotNull(Find(reuseTl, "rec.landing"));
            Assert.IsNull(Find(reuseTl, "rec.boostback"));
            Assert.IsNotNull(Absence(reuseTl, "rec.boostback"));
            Assert.IsNull(Find(reuseTl, "stg.ses.s2.2"));

            var heavy = cat.Preset("super-heavy");
            var heavyTl = Gen(cat, heavy);
            Assert.IsNotNull(Find(heavyTl, "ign.stagger"));
            Assert.IsNotNull(Find(heavyTl, "pad.strongback"));
            Assert.AreEqual("expendable", heavyTl.recovery);
            Assert.IsNull(Find(heavyTl, "rec.landing"));

            var stock = cat.Preset("stock");
            var stockTl = Gen(cat, stock);
            Assert.IsNotNull(Find(stockTl, "stg.booster"));
            Assert.IsNotNull(Find(stockTl, "asc.meco"));

            var noRestart = LoadTemplate("small-kerolox.json").Clone();
            noRestart.stages[1].engine = "sc-kerolox";
            noRestart.stages[1].propellant = "kerolox";
            var nr = Gen(cat, noRestart);
            Assert.IsNull(Find(nr, "stg.ses.s2.1"));
            Assert.IsNotNull(Find(nr, "stg.light.s2"));
            Assert.IsNotNull(Absence(nr, "stg.ses.s2.1"));

            var bare = LoadTemplate("small-kerolox.json");
            var noUmb = new Pad { id = "pad.bare", crewArms = false, strongback = false, umbilical = false, deluge = false, internalPower = true };
            var bareTl = TimelineGenerator.Generate(cat, bare, noUmb, new Mission { mode = "standard" }, 1);
            Assert.IsTrue(bareTl.checks.ok, string.Join(" | ", bareTl.checks.errors));
            Assert.IsNull(Find(bareTl, "count.lox.s1.load"));
            Assert.IsNotNull(Absence(bareTl, "count.load.s1"));
            Assert.IsNull(Find(bareTl, "pad.deluge"));
        }

        [Test]
        public void LatitudeHook_MatchesParamsShape()
        {
            var cat = Golden.Catalog;
            var d = LoadTemplate("small-kerolox.json");
            var pad = Pad.For(cat, d);
            var root = new JsonObject();
            var flight = new JsonObject();
            flight.Add("launchLatitude", 0d);
            root.Add("flight", flight);
            double lat = TimelineParams.LaunchLatitude(root);
            Assert.AreEqual(0, lat, 1e-12);
            var a = TimelineGenerator.Generate(cat, d, pad, new Mission { latitude = lat }, 1);
            var b = TimelineGenerator.Generate(cat, d, pad, new Mission { latitude = 28.5 }, 1);
            Assert.AreEqual(0, a.latitude, 1e-12);
            Assert.AreEqual(28.5, b.latitude, 1e-12);
            Assert.AreNotEqual(TimelineJson.Write(a), TimelineJson.Write(b));
            bool simMoved = Find(a, "asc.maxq").actual != Find(b, "asc.maxq").actual
                || Find(a, "asc.maxq").strings.explainWhy != Find(b, "asc.maxq").strings.explainWhy
                || Find(a, "asc.meco")?.actual != Find(b, "asc.meco")?.actual;
            Assert.IsTrue(simMoved, "latitude 0 vs 28.5 did not change the sim: " + Find(a, "asc.maxq").strings.explainWhy);
        }

        [Test]
        public void StatusBus_UsesOneSchema()
        {
            var cat = Golden.Catalog;
            var tl = Gen(cat, LoadTemplate("sounding-solid.json"));
            var liftoff = Find(tl, "term.liftoff");
            string json = TimelineJson.StatusEvent(liftoff);
            StringAssert.Contains("\"type\":\"event.status\"", json);
            StringAssert.Contains("\"id\":\"term.liftoff\"", json);
            StringAssert.Contains("\"status\":\"pending\"", json);
        }

        [Test]
        public void RandomVehicles_PassChecks_AndStayDeterministic()
        {
            var cat = Golden.Catalog;
            var bases = new List<Design>(AllVehicles());
            var rng = new Rng(0x51E1);
            string[] modes = { "kid", "standard", "engineer" };
            string[] recs = { null, "expendable", "droneship", "rtls", "parachute" };
            for (int i = 0; i < 24; i++)
            {
                var d = bases[rng.Next(bases.Count)].Clone();
                d.id = "rand-" + i;
                d.stages[0].propMass = Math.Max(800, d.stages[0].propMass * (0.6 + rng.NextDouble()));
                d.stages[0].engineCount = 1 + rng.Next(6);
                if (d.stages.Count > 1) d.stages[1].propMass = Math.Max(400, d.stages[1].propMass * (0.6 + rng.NextDouble()));
                if (rng.Next(3) == 0) { d.fairingLength = 0; d.fairingDiameter = 0; }
                var pad = new Pad
                {
                    id = "pad.rand",
                    crewArms = rng.Next(2) == 0,
                    strongback = rng.Next(2) == 0,
                    umbilical = rng.Next(4) != 0,
                    deluge = rng.Next(2) == 0,
                    internalPower = rng.Next(5) != 0,
                };
                var mission = new Mission
                {
                    mode = modes[rng.Next(modes.Length)],
                    recovery = recs[rng.Next(recs.Length)],
                    stageParachute = rng.Next(4) == 0,
                    fairingParachute = rng.Next(4) == 0,
                };
                uint seed = (uint)rng.Next(100000);
                var a = TimelineGenerator.Generate(cat, d, pad, mission, seed);
                var b = TimelineGenerator.Generate(cat, d, pad, mission, seed);
                Assert.AreEqual(TimelineJson.Write(a), TimelineJson.Write(b), d.id);
                Assert.IsTrue(a.checks.ok, d.id + ": " + string.Join(" | ", a.checks.errors));
                AssertMonotonic(a, d.id);
            }
        }

        [Test]
        public void Generation_IsFasterThanAFewMilliseconds()
        {
            var cat = Golden.Catalog;
            var vehicles = new List<Design>(AllVehicles());
            foreach (var d in vehicles) TimelineGenerator.Generate(cat, d, Pad.For(cat, d), new Mission(), 1);
            var best = new double[vehicles.Count];
            for (int i = 0; i < best.Length; i++) best[i] = double.PositiveInfinity;
            var watch = new Stopwatch();
            for (int pass = 0; pass < 5; pass++)
            {
                for (int i = 0; i < vehicles.Count; i++)
                {
                    watch.Restart();
                    TimelineGenerator.Generate(cat, vehicles[i], Pad.For(cat, vehicles[i]), new Mission(), 1);
                    watch.Stop();
                    double ms = watch.Elapsed.TotalMilliseconds;
                    if (ms < best[i]) best[i] = ms;
                }
            }
            Array.Sort(best);
            double median = best[best.Length / 2];
            double worst = best[best.Length - 1];
            TestContext.WriteLine("timeline ms best-of-5 median " + median.ToString("0.###") + " worst " + worst.ToString("0.###"));
            Assert.Less(median, 2.0, "typical generation");
            Assert.Less(worst, 5.0, "worst generation");
        }

        [Test]
        public void GoldenTimelines_MatchCommittedVectors()
        {
            var cat = Golden.Catalog;
            bool write = Environment.GetEnvironmentVariable("KESTREL_WRITE_TIMELINES") == "1";
            string dir = Path.Combine(Golden.Root, "tests", "vectors", "timeline");
            Directory.CreateDirectory(dir);
            string sampleDir = Path.Combine(dir, "samples");
            Directory.CreateDirectory(sampleDir);
            foreach (var d in AllVehicles())
            {
                var tl = Gen(cat, d);
                string json = TimelineJson.Write(tl);
                string path = Path.Combine(dir, d.id + ".json");
                if (write) File.WriteAllText(path, json);
                Assert.IsTrue(File.Exists(path), "missing golden " + path + " (set KESTREL_WRITE_TIMELINES=1 to create)");
                Assert.AreEqual(File.ReadAllText(path), json, d.id);
                if (Array.IndexOf(SampleIds, d.id) >= 0)
                {
                    string md = TimelineText.Markdown(tl);
                    string mdPath = Path.Combine(sampleDir, d.id + ".md");
                    if (write) File.WriteAllText(mdPath, md);
                    Assert.AreEqual(File.ReadAllText(mdPath), md, d.id + " sample");
                }
            }
        }

        static Kestrel.Core.Timeline.Timeline Gen(Catalog cat, Design d) => TimelineGenerator.Generate(cat, d, Pad.For(cat, d), new Mission { mode = "standard" }, 1);

        static void AssertMonotonic(Kestrel.Core.Timeline.Timeline t, string id)
        {
            for (int i = 1; i < t.events.Count; i++)
                Assert.LessOrEqual(t.events[i - 1].nominal, t.events[i].nominal + 1e-9, id + " " + t.events[i].id);
        }

        static string Ids(Kestrel.Core.Timeline.Timeline t)
        {
            var s = "";
            foreach (var e in t.events) s += e.id + "\n";
            return s;
        }

        static MissionEvent Find(Kestrel.Core.Timeline.Timeline t, string id)
        {
            foreach (var e in t.events) if (e.id == id) return e;
            return null;
        }

        static Absence Absence(Kestrel.Core.Timeline.Timeline t, string id)
        {
            foreach (var a in t.absent) if (a.id == id) return a;
            return null;
        }

        static Design LoadTemplate(string file)
        {
            string path = Path.Combine(Golden.Root, "web", "js", "data", "templates", file);
            return Design.FromJson(MiniJson.ParseObject(File.ReadAllText(path)));
        }

        static IEnumerable<Design> AllVehicles()
        {
            var dir = Path.Combine(Golden.Root, "web", "js", "data", "templates");
            var files = new List<string>(Directory.GetFiles(dir, "*.json"));
            files.Sort(StringComparer.Ordinal);
            foreach (var f in files) yield return Design.FromJson(MiniJson.ParseObject(File.ReadAllText(f)));
            var presets = new List<Design>(Golden.Catalog.Presets);
            presets.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            foreach (var p in presets) yield return p;
        }

        sealed class Rng
        {
            uint s;
            public Rng(uint seed) { s = seed == 0 ? 1u : seed; }
            public int Next(int n) { s = s * 1664525u + 1013904223u; return (int)(s % (uint)n); }
            public double NextDouble() { s = s * 1664525u + 1013904223u; return (s & 0xFFFFFF) / (double)0x1000000; }
        }
    }
}

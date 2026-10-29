// Thrum only says what is true of the rocket: the finless tumble line needs a rocket with no fins, and the
// "others could not cope" engine-out line needs other engines.
using System;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Guide;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Game.Guide.Tests
{
    public class GuideNarrationGatingTests
    {
        string json;
        string root;

        [SetUp]
        public void SetUp()
        {
            root = Repo();
            json = File.ReadAllText(Path.Combine(root, "data", "guide.json"));
            GuideSession.Bind(null);
        }

        static string Repo()
        {
            string[] starts = { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            for (int i = 0; i < starts.Length; i++)
            {
                try { return Catalog.FindRepoRoot(starts[i]); }
                catch (DirectoryNotFoundException) { }
            }
            throw new DirectoryNotFoundException("data/guide.json");
        }

        LiveGuideEngine Engine() => new LiveGuideEngine(json, GuideState.Fresh(false, true), root);

        static StepperReport Tumble(bool? finless)
        {
            var report = new StepperReport { Finless = finless };
            report.Checks[4] = new StepCheck { Code = "tumble", Gate = StepGate.Red };
            return report;
        }

        static bool Has(LiveGuideEngine engine, string id)
        {
            for (int i = 0; i < engine.Pending.Count; i++)
                if (engine.Pending[i] != null && engine.Pending[i].Id == id) return true;
            return false;
        }

        [Test]
        public void AFinlessTumbleKeepsTheFinlessLine()
        {
            var engine = Engine();
            GuideHooks.Designer(engine, 4, Tumble(true), false);
            Assert.IsTrue(Has(engine, "step.4.fail.tumble"));
            Assert.IsFalse(Has(engine, "pre.stability"));
        }

        [Test]
        public void AFinnedTumbleNeverHearsThatItHasNoFins()
        {
            var engine = Engine();
            GuideHooks.Designer(engine, 4, Tumble(false), false);
            Assert.IsFalse(Has(engine, "step.4.fail.tumble"), "that card says the rocket is finless");
            Assert.IsTrue(Has(engine, "pre.stability"), "the stability warning is true of any design");
        }

        [Test]
        public void AnUnknownDesignKeepsTheOldLine()
        {
            var engine = Engine();
            GuideHooks.Designer(engine, 4, Tumble(null), false);
            Assert.IsTrue(Has(engine, "step.4.fail.tumble"));
        }

        [Test]
        public void EngineOutNeedsOtherEngines()
        {
            foreach (var (lit, want) in new[] { (0, false), (1, false), (2, true), (9, true) })
            {
                var engine = Engine();
                var facts = GuideHooks.FromFlight(
                    new FlightDebrief { Passed = false, Kind = "engine-out", MissionId = "m4", Seed = 3 },
                    new SimResult { failure = new FailureEvent { kind = "engine-out", limit = lit } },
                    new MissionContext { MissionId = "m4" }, null);
                Assert.AreEqual(lit, facts.EnginesLit);
                engine.ShowReport(facts);
                Assert.AreEqual(want, Has(engine, "fail.engine-out"), "engines lit " + lit);
            }
        }

        [Test]
        public void OtherFailuresAreUntouchedByTheEngineCount()
        {
            var engine = Engine();
            engine.ShowReport(new GuideReport { Stamp = 5, Passed = false, MissionId = "m4", Kind = "tumble", MissionIndex = 4 });
            Assert.IsTrue(Has(engine, "fail.tumble"));
        }
    }
}

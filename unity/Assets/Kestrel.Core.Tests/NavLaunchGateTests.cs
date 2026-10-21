// F1 / NAV-01, NAV-02 (NAV_BUGS.md, test/nav-flow #308). Mirrors KnownBug_NAV01_DesignFirstRocket_LaunchGateBlocks
// and KnownBug_NAV02_GoldenWinners_DesignerGateDisagreesWithFlightBatch as plain tests: the designer gate, run the
// way the nav harness runs it (ReferenceAscent.Run, then LaunchGate.Evaluate), is Ready for every campaign winner
// and for every DESIGN FIRST rocket the campaign flight does not really fail. When #308 lands, its two KnownBug tests are removed in favour of these.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class NavLaunchGateTests
    {
        Catalog catalog;
        Campaign campaign;
        HangarData hangar;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            GameSession.Clear();
            PadHandoff.Clear();
            ReferenceAscent.Reset();
        }

        [TearDown]
        public void Clean()
        {
            GameSession.Clear();
            PadHandoff.Clear();
            ReferenceAscent.Reset();
        }

        LaunchGateResult DesignerGate(JsonObject json)
        {
            try { ReferenceAscent.Run(catalog, json); }
            catch (Exception e) { return new LaunchGateResult { state = LaunchGateState.Blocked, code = "reference-threw", reason = e.Message }; }
            return LaunchGate.Evaluate(Validate.Engineering(catalog, json), Readout.Quick(catalog, json));
        }

        /// <summary>
        /// Every DESIGN FIRST rocket is Ready, unless the campaign flight (FlightBatch, seeds 1-5, the same
        /// MissionFlightSetup as the pad) really fails it the same way on every seed. Then the gate says so with the
        /// real time, and Launch still flies after one confirm. Today that is the KH-11 and KH-13 board starters
        /// (board-starters.json mode "fix": one strength step short until the player taps Fix it).
        /// </summary>
        [Test]
        public void NAV01_DesignFirstRocket_IsReadyInTheDesigner()
        {
            var bad = new List<string>();
            var real = new List<string>();
            for (int cleared = 0; cleared < 20; cleared++)
            {
                var r = HomeRoutes.DesignFirst(campaign, cleared, catalog, hangar.templates);
                if (!PadHandoff.TryTake(out var json, out _)) { bad.Add("m" + (cleared + 1) + " no rocket"); continue; }
                var gate = DesignerGate(json);
                if (gate.Ready) continue;
                string mid = r.Session.MissionId;
                string flown = BatchFailure(campaign.ById(mid), json);
                if (flown != null && flown == gate.code && gate.CanLaunch && gate.NeedsConfirm && !gate.Line.Contains("T+0 s"))
                {
                    real.Add(mid + " " + gate.code);
                    continue;
                }
                bad.Add(mid + " " + gate.state + " " + gate.code + " (FlightBatch: " + (flown ?? "flies 5/5") + "): " + gate.Line.Split('.')[0]);
            }
            Assert.That(bad, Is.Empty, bad.Count + "/20 blocked. " + string.Join(" | ", bad));
            TestContext.WriteLine("Real predicted failures (Launch anyway): " + string.Join(", ", real));
        }

        /// <summary>The failure kind when FlightBatch fails <paramref name="json"/> on every campaign seed 1-5 the same way, else null.</summary>
        string BatchFailure(CampaignMission mission, JsonObject json)
        {
            var design = Kestrel.Core.Vehicle.Design.FromJson(json);
            string kind = null;
            for (ulong seed = 1; seed <= 5; seed++)
            {
                var f = FlightBatch.Fly(catalog, mission, design, seed);
                if (f.failure == null) return null;
                if (kind != null && kind != f.failure) return null;
                kind = f.failure;
            }
            return kind;
        }

        [Test]
        public void NAV02_GoldenWinners_AreReadyInTheDesigner()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            var bad = new List<string>();
            foreach (JsonObject row in file.Arr("missions"))
            {
                var json = JsonMut.CloneObject((JsonObject)row.Arr("designs")[0]);
                var gate = DesignerGate(json);
                if (!gate.Ready) bad.Add(row.Str("id") + " " + gate.code + ": " + gate.Line);
            }
            Assert.That(bad, Is.Empty, "blocked: " + string.Join(" | ", bad));
        }
    }
}

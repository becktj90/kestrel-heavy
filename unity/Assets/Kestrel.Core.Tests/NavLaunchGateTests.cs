// F1 / NAV-01, NAV-02 (NAV_BUGS.md, test/nav-flow #308). Mirrors KnownBug_NAV01_DesignFirstRocket_LaunchGateBlocks
// and KnownBug_NAV02_GoldenWinners_DesignerGateDisagreesWithFlightBatch as plain tests: the designer gate, run the
// way the nav harness runs it (ReferenceAscent.Run, then LaunchGate.Evaluate), is Ready for every campaign winner
// and for every DESIGN FIRST rocket, which also flies FlightBatch seeds 1-5 with no failure. #308's two KnownBug tests were removed in favour of these.
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
        /// Every DESIGN FIRST rocket is Ready in the designer and flies in FlightBatch (seeds 1-5, the same
        /// MissionFlightSetup as the pad) without a failure on any seed. Our own starters always fly; only the
        /// player's own edits can make a rocket break.
        /// </summary>
        [Test]
        public void NAV01_DesignFirstRocket_IsReadyInTheDesignerAndFlies()
        {
            var bad = new List<string>();
            for (int cleared = 0; cleared < 20; cleared++)
            {
                GameSession.Clear();
                PadHandoff.Clear();
                ReferenceAscent.Reset();
                var r = HomeRoutes.DesignFirst(campaign, cleared, catalog, hangar.templates);
                if (!PadHandoff.TryTake(out var json, out _)) { bad.Add("m" + (cleared + 1) + " no rocket"); continue; }
                string mid = r.Session.MissionId;
                var flown = Failures(campaign.ById(mid), json);
                var gate = DesignerGate(json);
                if (!gate.Ready) bad.Add(mid + " " + gate.state + " " + gate.code + ": " + gate.Line.Split('.')[0]);
                if (flown.Count > 0) bad.Add(mid + " FlightBatch " + string.Join(",", flown));
            }
            Assert.That(bad, Is.Empty, bad.Count + " problems. " + string.Join(" | ", bad));
        }

        /// <summary>
        /// A player-made weak rocket still fails: the KH-11 starter with strength dropped to 1.55 bends at max-Q on
        /// every seed. The designer says so with the real time, and Launch still flies after one confirm.
        /// </summary>
        [Test]
        public void NAV01_PlayerWeakenedRocket_StillFails_AndCanLaunchAnyway()
        {
            HomeRoutes.DesignFirst(campaign, 10, catalog, hangar.templates);
            Assert.That(PadHandoff.TryTake(out var json, out _), "KH-11 DESIGN FIRST rocket");
            Assert.AreEqual("m11", json.Str("missionId"));
            json["safetyFactor"] = 1.55;
            var mission = campaign.ById("m11");
            Assert.AreEqual("structural-maxq", BatchFailure(mission, json), "a weak rocket must still break up");
            var gate = DesignerGate(json);
            Assert.IsFalse(gate.Ready, gate.Line);
            Assert.AreEqual("structural-maxq", gate.code);
            Assert.That(gate.CanLaunch && gate.NeedsConfirm, gate.state + ": " + gate.Line);
            StringAssert.DoesNotContain("T+0 s", gate.Line);
        }

        /// <summary>Every failure kind FlightBatch reports for <paramref name="json"/> on campaign seeds 1-5.</summary>
        List<string> Failures(CampaignMission mission, JsonObject json)
        {
            var design = Kestrel.Core.Vehicle.Design.FromJson(json);
            var list = new List<string>();
            for (ulong seed = 1; seed <= 5; seed++)
            {
                var f = FlightBatch.Fly(catalog, mission, design, seed);
                if (f.failure != null) list.Add("seed " + seed + " " + f.failure);
            }
            return list;
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

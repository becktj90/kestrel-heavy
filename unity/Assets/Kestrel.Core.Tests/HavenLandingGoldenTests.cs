// Flown Haven recoveries from the campaign winners. The ascent grade is only a
// prediction. These tests fly the booster from MECO to the surface.
// Assist can make a feasible return. An idle stick and a stuck burn do not. KH-13's winner used to be a lofted
// 139 km / 3.3 km/s miss on a 4% reserve; t-booster-land rebuilt it (13% / 28 t reserve, MECO near 90 km) and it lands.
// These states are the bare sim's inertial burnout; BoosterLandingMissionTests flies the live ground-frame hand-over.
// An empty checkout still flies off the pack already in memory.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HavenLandingGoldenTests
    {
        static readonly string[] Feasible = { "m6", "m7", "m10", "m13", "m15", "m19" };

        [Test]
        public void Assisted_LandsTheFeasibleReturns_AndReplaysBitIdentical()
        {
            var flown = FlyAll();
            foreach (string id in Feasible)
            {
                var row = flown[id];
                var v = row.Auto;
                Assert.IsNull(v.Cause, id + " " + Dump(v));
                Assert.That(v.GradeName, Is.EqualTo("Good").Or.EqualTo("Perfect"), id);
                Assert.That(v.Medal, Is.EqualTo("silver").Or.EqualTo("gold"), id);
                Assert.LessOrEqual(Math.Abs(v.MissM), RecoverySim.DeckRadiusM, id);
                Assert.LessOrEqual(v.Sink, row.Craft.VLeg, id);
                Assert.Greater(v.Sink, 0.2, id + " still comes down");
                Assert.LessOrEqual(v.TiltDeg, RecoverySim.TiltLimitDeg, id);
                Assert.Greater(v.PropLeft, 200, id + " keeps landing fuel");
                var again = RecoverySim.Fly(row.Craft, row.Down, row.Alt, row.East, row.Up, RecoveryStick.Assisted);
                Assert.AreEqual(v.MissM, again.MissM, 0, id);
                Assert.AreEqual(v.Sink, again.Sink, 0, id);
                Assert.AreEqual(v.T, again.T, 0, id);
                Assert.AreEqual(v.PropLeft, again.PropLeft, 0, id);
                Assert.AreEqual(v.GradeName, again.GradeName, id);
            }
        }

        [Test]
        public void IdleAndFullThrottle_MissTheDeck()
        {
            var flown = FlyAll();
            var up = new RecoveryStick { Auto = false, Throttle = 1, PitchRad = Math.PI / 2 };
            foreach (string id in Feasible)
            {
                var row = flown[id];
                AssertMiss(id + " idle", row.Idle);
                var stuck = RecoverySim.Fly(row.Craft, row.Down, row.Alt, row.East, row.Up, up);
                Assert.IsNotNull(stuck.Cause, id + " full throttle " + Dump(stuck));
                Assert.IsFalse(MissionScore.IsGood(stuck.GradeName), id + " full throttle " + Dump(stuck));
                Assert.AreNotEqual("gold", stuck.Medal, id);
                Assert.AreNotEqual("silver", stuck.Medal, id);
            }
        }

        [Test]
        public void FlockYard_RebuiltWinner_LandsOnAnHonestReserve()
        {
            // t-booster-land: the old winner separated at 139 km and 3.3 km/s on a 4% reserve and could not come back.
            // No altitude is asserted: the cutoff altitude is a symptom of the staging split. What matters is that the booster
            // comes home, at a separation speed the landing starters accept, on a reserve that is neither short nor padded.
            var row = FlyAll()["m13"];

            // (a) The assisted return flies clean and grades well; an idle stick still misses.
            Assert.IsNull(row.Auto.Cause, Dump(row.Auto));
            Assert.IsTrue(MissionScore.IsGood(row.Auto.GradeName), Dump(row.Auto));
            AssertMiss("m13 idle", row.Idle);

            // (b) Ground-frame separation speed inside the band BoosterLandingMissionTests holds every landing starter to.
            var flight = FlightBatch.Fly(row.Catalog, row.Mission, row.Design, row.Seed);
            Assert.That(flight.separationSpeedMps, Is.GreaterThan(BoosterLandingMissionTests.SeparationSpeedMinMps)
                .And.LessThan(BoosterLandingMissionTests.SeparationSpeedMaxMps), "separation speed " + flight.separationSpeedMps.ToString("0") + " m/s");

            // (c) Reserve delta-v (rocket equation from the design's reserve mass, stage dry mass and Isp) against the delta-v the
            // flown return really used (same equation on the propellant it burned: reserve minus what was left at the end).
            double isp = row.Craft.IspSec, dry = row.Craft.DryKg;
            double dvReserve = RecoveryModel.ReserveDeltaV(isp, dry, row.Craft.ReserveKg);
            double dvUsed = isp * Atmosphere.G0 * Math.Log((dry + row.Craft.ReserveKg) / (dry + row.Auto.PropLeft));
            string why = "reserve " + dvReserve.ToString("0") + " m/s vs flown return " + dvUsed.ToString("0") + " m/s (x" + (dvReserve / dvUsed).ToString("0.00") + ")";
            TestContext.WriteLine("m13 " + why + ", separation " + flight.separationSpeedMps.ToString("0") + " m/s, flown propellant left " + row.Auto.PropLeft.ToString("0") + " kg");
            Assert.Greater(dvUsed, 0, why);
            // 1.10 is about the 10% propellant reserve a real landing burn carries; 1.5 is where margin stops being margin and starts
            // hiding a design problem. Both are engineering judgment, not derived constants.
            Assert.GreaterOrEqual(dvReserve, 1.10 * dvUsed, "reserve is short of a 10% landing margin: " + why);
            Assert.LessOrEqual(dvReserve, 1.5 * dvUsed, "reserve is padded past 1.5x, check the design instead: " + why);
        }

        [Test]
        public void RecoveryCallout_FitsTheNarrowWidth()
        {
            string line = FlightStaging.Format("LANDING BURN", 520, 137900, 3380);
            string touch = RecoveryViews.Touchdown(19.4, 8.0, 36);
            float room = 335f - 2f * FlightHudLayout.SideMarginPt;
            Assert.Less(FlightHudLayout.TextWidth(line, FlightHudLayout.LabelFontPt), room, line);
            Assert.Less(FlightHudLayout.TextWidth(touch, FlightHudLayout.LabelFontPt), room, touch);
            Assert.That(line, Does.Not.Contain("\n"));
            Assert.That(touch, Does.Not.Contain("\n"));
        }

        [Test]
        public void DevicePack_FliesWhenTheCheckoutIsGone()
        {
            var catalog = Catalog.LoadFromRepo(Golden.Root);
            var campaign = Campaign.Load(Golden.Root);
            string winners = File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json"));
            string portrait = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Resources", "Guide", "portrait.json"));
            string glyphs = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Resources", "Guide", "glyphs.json"));
            string prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            string away = Path.Combine(Path.GetTempPath(), "kh-haven-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(away);
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", away);
            try
            {
                Assert.IsNull(Catalog.RepoRootOrNull());
                Assert.IsFalse(File.Exists(Path.Combine(away, "data", "campaign.json")));
                var row = Capture(catalog, campaign, winners, "m7");
                var auto = RecoverySim.Fly(row.Craft, row.Down, row.Alt, row.East, row.Up, RecoveryStick.Assisted);
                Assert.IsNull(auto.Cause, Dump(auto));
                Assert.LessOrEqual(Math.Abs(auto.MissM), RecoverySim.DeckRadiusM);
                Assert.That(auto.Medal, Is.EqualTo("silver").Or.EqualTo("gold"));

                var face = MiniJson.ParseObject(portrait);
                Assert.AreEqual(16, face.Num("w"), 0);
                Assert.AreEqual(16, face.Num("h"), 0);
                var palette = face.Obj("palette");
                Assert.AreEqual("#1B1F2AFF", palette.Str("o"), "ink outline stays");
                Assert.AreEqual("#E8B15AFF", palette.Str("g"), "amber badge stays");
                Assert.AreEqual("#C04858FF", palette.Str("r"), "coveralls take the dusk ember");
                Assert.AreEqual("#E87850FF", palette.Str("b"), "the smile takes the dusk horizon");
                var font = MiniJson.ParseObject(glyphs);
                Assert.AreEqual(6, font.Num("cellW"), 0);
                Assert.AreEqual(7, font.Num("glyphH"), 0);
                Assert.AreEqual(96, font.Arr("glyphs").Count, "Resources bitmap font");
                float caption = 335f * 0.5f - 8f;
                Assert.Less("Mind the oil.".Length * font.Num("cellW"), caption);
            }
            finally
            {
                Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prev);
                if (Directory.Exists(away)) Directory.Delete(away, true);
            }
        }

        static void AssertMiss(string id, RecoveryVerdict v)
        {
            Assert.AreEqual(RecoveryScore.Miss, v.Cause, id + " " + Dump(v));
            Assert.IsNull(v.Medal, id);
            Assert.Greater(Math.Abs(v.MissM), RecoverySim.DeckRadiusM, id);
        }

        static Dictionary<string, Row> FlyAll()
        {
            var catalog = Catalog.LoadFromRepo(Golden.Root);
            var campaign = Campaign.Load(Golden.Root);
            string winners = File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json"));
            var map = new Dictionary<string, Row>();
            foreach (string id in new[] { "m6", "m7", "m10", "m13", "m15", "m19" })
                map[id] = Capture(catalog, campaign, winners, id);
            return map;
        }

        static Row Capture(Catalog catalog, Campaign campaign, string winners, string id)
        {
            var file = MiniJson.ParseObject(winners);
            JsonObject doc = null;
            foreach (JsonObject row in file.Arr("missions"))
            {
                if (row.Str("id") == id) doc = (JsonObject)row.Arr("designs")[0];
            }
            Assert.IsNotNull(doc, id);
            int index = int.Parse(id.Substring(1));
            var mission = campaign.Mission(index);
            Assert.IsTrue(mission.LandingRequired, id);
            var design = Design.FromJson(doc);
            Assert.IsTrue(RecoveryCrafts.TryFrom(catalog, design, out var craft), id);
            var input = Stack.SimInputs(catalog, design, 0.05, 0);
            input.opts.targetAlt = mission.GuidanceAltM;
            if (input.opts.guidance != null) input.opts.guidance.targetAlt = mission.GuidanceAltM;
            int core = design.StrapOnCount > 0 ? 2 : 1;
            var sim = new AscentSim(input.stages, input.opts);
            bool caught = false;
            double down = 0, alt = 0, east = 0, up = 0;
            int guard = 0;
            while (!sim.Done && guard++ < 200000)
            {
                sim.Tick();
                for (int i = 0; i < sim.Events.Count; i++)
                {
                    var e = sim.Events[i];
                    if (e.type == "burnout" && e.stage == core)
                    {
                        caught = true;
                        Mecco(sim.Y, out down, out alt, out east, out up);
                    }
                }
                if (caught) break;
            }
            Assert.IsTrue(caught, id + " core burnout");
            return new Row
            {
                Catalog = catalog,
                Mission = mission,
                Design = design,
                Seed = campaign.Seeds[0],
                Craft = craft,
                Down = down,
                Alt = alt,
                East = east,
                Up = up,
                Auto = RecoverySim.Fly(craft, down, alt, east, up, RecoveryStick.Assisted),
                Idle = RecoverySim.Fly(craft, down, alt, east, up, new RecoveryStick { Auto = false, Throttle = 0, PitchRad = Math.PI / 2 }),
            };
        }

        static void Mecco(double[] y, out double down, out double alt, out double east, out double up)
        {
            double x = y[0], yy = y[1], vx = y[2], vy = y[3];
            double r = Math.Sqrt(x * x + yy * yy);
            alt = r - Gravity.R_EARTH;
            double ux = x / r, uy = yy / r;
            east = vx * (-uy) + vy * ux;
            up = vx * ux + vy * uy;
            double arc = Math.Atan2(x, yy) * Gravity.R_EARTH;
            down = east >= 0 ? -arc : arc;
            if (east < 0) east = -east;
        }

        static string Dump(RecoveryVerdict v)
        {
            return (v.Cause ?? "ok") + " " + v.GradeName + " " + v.Medal
                + " sink=" + v.Sink.ToString("0.00") + " tilt=" + v.TiltDeg.ToString("0.0")
                + " miss=" + v.MissM.ToString("0") + " prop=" + v.PropLeft.ToString("0");
        }

        sealed class Row
        {
            public Catalog Catalog;
            public CampaignMission Mission;
            public Design Design;
            public ulong Seed;
            public RecoveryCraft Craft;
            public double Down, Alt, East, Up;
            public RecoveryVerdict Auto, Idle;
        }
    }
}
